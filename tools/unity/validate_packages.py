#!/usr/bin/env python3
"""Check the Unity package manifest against tools/unity/package_policy.json without opening Unity.

Offline checks (default, used by CI):
  - every required package is a direct dependency at exactly the pinned version
  - no forbidden package (Oculus XR plugin, Meta integration SDKs), direct or, once Unity has
    written packages-lock.json, transitive
  - no git/file/URL package sources and no scoped registries outside the policy's allow list
  - ProjectVersion.txt, the policy and QuestBuildConfig.cs agree on the editor version
  - QuestBuildConfig.cs forbids exactly the policy's forbidden prefixes

--online additionally asks packages.unity.com whether each pinned (non-core) version exists and
supports Unity 6000.3, and reports newer releases. It never edits anything.

Usage:
  python tools/unity/validate_packages.py [--root .] [--online]
Exit code: 0 pass, 1 errors found.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
import urllib.error
import urllib.request
from dataclasses import dataclass
from pathlib import Path

POLICY = Path("tools/unity/package_policy.json")
MANIFEST = Path("Game/Packages/manifest.json")
LOCK = Path("Game/Packages/packages-lock.json")
PROJECT_VERSION = Path("Game/ProjectSettings/ProjectVersion.txt")
QUEST_CONFIG = Path("Game/Assets/PatientZero/Integration/Logic/QuestBuildConfig.cs")
SEMVER = re.compile(r"^\d+\.\d+\.\d+(-[0-9A-Za-z.\-]+)?$")


@dataclass
class Finding:
    severity: str  # ERROR, WARN, INFO, PASS
    rule: str
    message: str

    def __str__(self) -> str:
        return f"{self.severity:5} {self.rule}: {self.message}"


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def is_forbidden(name: str, prefixes: list[str]) -> bool:
    return any(name == p or name.startswith(p + ".") for p in prefixes)


def editor_of(project_version: str) -> tuple[str | None, str | None]:
    version = re.search(r"^m_EditorVersion:\s*(\S+)\s*$", project_version, re.M)
    revision = re.search(r"^m_EditorVersionWithRevision:\s*\S+\s*\(([0-9a-f]+)\)\s*$", project_version, re.M)
    return (version.group(1) if version else None, revision.group(1) if revision else None)


def quest_config_values(source: str) -> tuple[str | None, list[str] | None]:
    editor = re.search(r'EditorVersion\s*=\s*"([^"]+)"', source)
    block = re.search(r"ForbiddenPackagePrefixes\s*\{\s*get;\s*\}\s*=\s*Array\.AsReadOnly\(\s*new\[\]\s*\{(.*?)\}", source, re.S)
    prefixes = re.findall(r'"([^"]+)"', block.group(1)) if block else None
    return (editor.group(1) if editor else None, prefixes)


def check(root: Path) -> list[Finding]:
    f: list[Finding] = []
    for rel in (POLICY, MANIFEST, PROJECT_VERSION, QUEST_CONFIG):
        if not (root / rel).exists():
            f.append(Finding("ERROR", "missing-file", f"{rel.as_posix()} not found"))
    if any(x.rule == "missing-file" for x in f):
        return f

    policy = load_json(root / POLICY)
    manifest = load_json(root / MANIFEST)
    deps: dict[str, str] = manifest.get("dependencies", {})
    forbidden: list[str] = policy["forbiddenPrefixes"]

    for pin in policy["required"]:
        name, version = pin["name"], pin["version"]
        if name not in deps:
            f.append(Finding("ERROR", "package-missing", f"{name} {version} is not in manifest.json"))
        elif deps[name] != version:
            f.append(Finding("ERROR", "package-version", f"{name} is {deps[name]} in manifest.json, policy pins {version}"))
        else:
            f.append(Finding("PASS", "package", f"{name} {version}"))

    for name, value in sorted(deps.items()):
        if is_forbidden(name, forbidden):
            f.append(Finding("ERROR", "package-forbidden", f"{name} is a direct dependency; the project uses Unity OpenXR, not the Oculus/Meta integration SDK"))
        if not SEMVER.match(value):
            f.append(Finding("ERROR", "package-source", f"{name} uses '{value}'; only registry versions are reproducible on every seat and in CI"))

    allowed = {r.get("url") for r in policy.get("allowedScopedRegistries", [])}
    for registry in manifest.get("scopedRegistries", []):
        if registry.get("url") not in allowed:
            f.append(Finding("ERROR", "scoped-registry", f"{registry.get('name')} ({registry.get('url')}) is not allowed by the policy"))

    lock_path = root / LOCK
    if lock_path.exists():
        lock = load_json(lock_path).get("dependencies", {})
        for name, entry in sorted(lock.items()):
            if is_forbidden(name, forbidden):
                f.append(Finding("ERROR", "package-forbidden", f"{name} {entry.get('version')} is resolved in packages-lock.json (depth {entry.get('depth')})"))
        for pin in policy["required"]:
            entry = lock.get(pin["name"])
            if entry and entry.get("version") != pin["version"]:
                f.append(Finding("ERROR", "lock-version", f"{pin['name']} resolved to {entry.get('version')}, policy pins {pin['version']}"))
        f.append(Finding("PASS", "lock", f"packages-lock.json checked ({len(lock)} resolved packages)"))
    else:
        f.append(Finding("INFO", "lock", "no packages-lock.json yet (written when Unity first opens Game/); transitive packages not checked"))

    editor, revision = editor_of((root / PROJECT_VERSION).read_text(encoding="utf-8"))
    if editor != policy["editor"]:
        f.append(Finding("ERROR", "editor-version", f"ProjectVersion.txt has {editor}, policy pins {policy['editor']}"))
    elif revision != policy.get("editorRevision"):
        f.append(Finding("ERROR", "editor-revision", f"ProjectVersion.txt revision {revision}, policy {policy.get('editorRevision')}"))
    else:
        f.append(Finding("PASS", "editor-version", f"{editor} ({revision})"))

    cs_editor, cs_forbidden = quest_config_values((root / QUEST_CONFIG).read_text(encoding="utf-8"))
    if cs_editor != policy["editor"]:
        f.append(Finding("ERROR", "config-drift", f"QuestBuildConfig.EditorVersion is {cs_editor}, policy pins {policy['editor']}"))
    if cs_forbidden is None or sorted(cs_forbidden) != sorted(forbidden):
        f.append(Finding("ERROR", "config-drift", f"QuestBuildConfig.ForbiddenPackagePrefixes {cs_forbidden} differs from the policy {forbidden}"))
    if cs_editor == policy["editor"] and cs_forbidden is not None and sorted(cs_forbidden) == sorted(forbidden):
        f.append(Finding("PASS", "config", "QuestBuildConfig matches the policy"))

    return f


def unity_supported(required: str | None, editor: str) -> bool:
    """A package's "unity" field ("2022.3", "6000.0") is the minimum editor it supports."""
    if not required:
        return True
    def key(v: str) -> tuple[int, ...]:
        return tuple(int(x) for x in re.findall(r"\d+", v)[:2])
    return key(required) <= key(editor)


def check_online(root: Path, fetch=None) -> list[Finding]:
    policy = load_json(root / POLICY)
    registry = policy.get("registry", "https://packages.unity.com").rstrip("/")
    fetch = fetch or (lambda url: json.loads(urllib.request.urlopen(url, timeout=20).read().decode("utf-8")))
    f: list[Finding] = []
    for pin in policy["required"]:
        name, version = pin["name"], pin["version"]
        if pin.get("editorLocked"):
            f.append(Finding("INFO", "online", f"{name} {version} is a core package fixed by the editor; checked in Unity by ProjectValidator"))
            continue
        try:
            doc = fetch(f"{registry}/{name}")
        except (urllib.error.URLError, OSError, ValueError) as e:
            f.append(Finding("WARN", "online", f"{name}: registry unreachable ({e})"))
            continue
        versions = doc.get("versions", {})
        if version not in versions:
            f.append(Finding("ERROR", "online-missing", f"{name} {version} is not published on {registry}"))
            continue
        needs = versions[version].get("unity")
        if not unity_supported(needs, policy["editor"]):
            f.append(Finding("ERROR", "online-unity", f"{name} {version} needs Unity {needs}"))
        latest = doc.get("dist-tags", {}).get("latest")
        note = f" (latest {latest})" if latest and latest != version else ""
        f.append(Finding("PASS", "online", f"{name} {version} published, needs Unity {needs or '?'}{note}"))
    return f


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--root", default=Path(__file__).resolve().parents[2], type=Path)
    parser.add_argument("--online", action="store_true", help="also query packages.unity.com")
    parser.add_argument("--quiet", action="store_true", help="print only problems and the summary")
    args = parser.parse_args(argv)

    findings = check(args.root)
    if args.online and not any(x.rule == "missing-file" for x in findings):
        findings += check_online(args.root)
    for x in findings:
        if not args.quiet or x.severity in ("ERROR", "WARN"):
            print(x)
    errors = sum(x.severity == "ERROR" for x in findings)
    warns = sum(x.severity == "WARN" for x in findings)
    print(f"{errors} error(s), {warns} warning(s).")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
