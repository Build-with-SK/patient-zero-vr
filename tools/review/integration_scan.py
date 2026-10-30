#!/usr/bin/env python3
"""Offline integration scan of the whole Unity tree: the problems that only show up when both seats' code compiles together.

check_rules.py reviews one seat's diff against the contract. This scan reads the merged tree and finds what would
break the first Unity compile or the first composed scene:

  ERROR unknown-reference       asmdef references an assembly no asmdef or pinned package provides
  ERROR player-refs-editor      a player assembly references an Editor-only assembly
  ERROR duplicate-assembly      two asmdefs with one name
  ERROR cycle                   asmdef reference cycle
  ERROR test-asmdef             test assembly without NUnit/TestRunner references or the UNITY_INCLUDE_TESTS constraint
  ERROR duplicate-type          one full type name declared twice in an assembly, or in two assemblies
  ERROR unity-object-file-name  MonoBehaviour/ScriptableObject whose file name differs from the class (Unity loses the script on save)
  ERROR using-without-reference `using X;` where no referenced assembly provides namespace X
  ERROR editor-api-in-player    UnityEditor outside #if UNITY_EDITOR in a player assembly (breaks the Android build)
  ERROR service-unregistered    Services<T> read somewhere, registered nowhere
  WARN  event-no-writer         a GameEvents channel is subscribed but never raised
  WARN  event-no-reader         a GameEvents channel is raised but never subscribed
  WARN  using-unknown-namespace a `using` this scan cannot attribute to an assembly

Package assembly names come from the packages' own asmdefs (PACKAGE_ASSEMBLIES); the first editor run confirms them.

  python tools/review/integration_scan.py            # scan the repository; exit 1 on any ERROR
  python tools/review/integration_scan.py --wiring   # also print the event and service wiring tables
  python tools/review/integration_scan.py --self-test
"""
from __future__ import annotations

import argparse
import json
import re
import sys
import tempfile
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
PZ = Path("Game/Assets/PatientZero")

# assembly name -> package that provides it
PACKAGE_ASSEMBLIES = {
    "UnityEngine.UI": "com.unity.ugui", "UnityEditor.UI": "com.unity.ugui",
    "Unity.TextMeshPro": "com.unity.ugui", "Unity.TextMeshPro.Editor": "com.unity.ugui",
    "Unity.XR.Interaction.Toolkit": "com.unity.xr.interaction.toolkit",
    "Unity.XR.Interaction.Toolkit.Editor": "com.unity.xr.interaction.toolkit",
    "Unity.XR.CoreUtils": "com.unity.xr.core-utils", "Unity.XR.CoreUtils.Editor": "com.unity.xr.core-utils",
    "Unity.InputSystem": "com.unity.inputsystem",
    "Unity.XR.Hands": "com.unity.xr.hands", "Unity.XR.Hands.Editor": "com.unity.xr.hands",
    "Unity.XR.Management": "com.unity.xr.management", "Unity.XR.Management.Editor": "com.unity.xr.management",
    "Unity.XR.OpenXR": "com.unity.xr.openxr", "Unity.XR.OpenXR.Editor": "com.unity.xr.openxr",
    "Unity.AI.Navigation": "com.unity.ai.navigation", "Unity.AI.Navigation.Editor": "com.unity.ai.navigation",
    "Unity.RenderPipelines.Universal.Runtime": "com.unity.render-pipelines.universal",
    "Unity.RenderPipelines.Universal.Editor": "com.unity.render-pipelines.universal",
    "Unity.RenderPipelines.Core.Runtime": "com.unity.render-pipelines.core",
    "Unity.RenderPipelines.Core.Editor": "com.unity.render-pipelines.core",
    "UnityEngine.TestRunner": "com.unity.test-framework", "UnityEditor.TestRunner": "com.unity.test-framework",
    "Unity.Mathematics": "com.unity.mathematics",
}
# Not pinned in manifest.json; arrive as dependencies of pinned packages (XRI, URP).
TRANSITIVE_PACKAGES = {"com.unity.ugui", "com.unity.xr.core-utils", "com.unity.render-pipelines.core", "com.unity.mathematics"}
EDITOR_ONLY_PACKAGE_ASSEMBLIES = {a for a in PACKAGE_ASSEMBLIES if a.endswith(".Editor") or a.startswith("UnityEditor.")}
# namespace prefix -> assembly that must be referenced (most specific first)
NAMESPACE_ASSEMBLIES = [
    ("UnityEngine.UI", "UnityEngine.UI"), ("UnityEngine.EventSystems", "UnityEngine.UI"), ("TMPro", "Unity.TextMeshPro"),
    ("UnityEditor.XR.Interaction.Toolkit", "Unity.XR.Interaction.Toolkit.Editor"),
    ("UnityEngine.XR.Interaction.Toolkit", "Unity.XR.Interaction.Toolkit"),
    ("Unity.XR.CoreUtils", "Unity.XR.CoreUtils"), ("UnityEngine.InputSystem", "Unity.InputSystem"),
    ("UnityEngine.XR.Hands", "Unity.XR.Hands"),
    ("UnityEditor.XR.Management", "Unity.XR.Management.Editor"), ("UnityEngine.XR.Management", "Unity.XR.Management"),
    ("UnityEditor.XR.OpenXR", "Unity.XR.OpenXR.Editor"), ("UnityEngine.XR.OpenXR", "Unity.XR.OpenXR"),
    ("Unity.AI.Navigation.Editor", "Unity.AI.Navigation.Editor"), ("Unity.AI.Navigation", "Unity.AI.Navigation"),
    ("UnityEngine.Rendering.Universal", "Unity.RenderPipelines.Universal.Runtime"),
    ("UnityEngine.TestTools", "UnityEngine.TestRunner"), ("NUnit.Framework", "nunit.framework.dll"),
]
ENGINE_NAMESPACES = ("System", "UnityEngine", "UnityEditor", "Unity.Collections", "Unity.Profiling", "Unity.Jobs")
UNITY_OBJECT_BASES = {"MonoBehaviour", "ScriptableObject", "StateMachineBehaviour"}


@dataclass
class Finding:
    severity: str
    rule: str
    where: str
    message: str

    def __str__(self) -> str:
        return f"{self.severity:5s} {self.rule}: {self.where}: {self.message}"


def strip_comments_and_strings(text: str) -> str:
    text = re.sub(r"/\*.*?\*/", lambda m: "\n" * m.group(0).count("\n"), text, flags=re.S)
    text = re.sub(r"//[^\n]*", "", text)
    return re.sub(r'@"(?:[^"]|"")*"|"(?:\\.|[^"\\\n])*"', '""', text)


def scan(root: Path, tables: dict | None = None) -> list[Finding]:
    findings: list[Finding] = []
    game = root / PZ
    manifest_path = root / "Game/Packages/manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))["dependencies"] if manifest_path.exists() else {}

    asmdefs: dict[str, dict] = {}
    for p in sorted(game.rglob("*.asmdef")):
        d = json.loads(p.read_text(encoding="utf-8-sig"))
        rel = p.relative_to(root).as_posix()
        if d["name"] in asmdefs:
            findings.append(Finding("ERROR", "duplicate-assembly", rel, f"{d['name']} is also defined in {asmdefs[d['name']]['rel']}"))
            continue
        d["rel"], d["dir"] = rel, p.parent
        asmdefs[d["name"]] = d

    def editor_only(name: str) -> bool:
        return name in EDITOR_ONLY_PACKAGE_ASSEMBLIES or asmdefs.get(name, {}).get("includePlatforms") == ["Editor"]

    for name, d in asmdefs.items():
        refs = d.get("references", [])
        for ref in refs:
            if ref.startswith("GUID:"):
                continue  # check_rules.py warns about GUID references
            if ref not in asmdefs:
                pkg = PACKAGE_ASSEMBLIES.get(ref)
                if pkg is None:
                    findings.append(Finding("ERROR", "unknown-reference", d["rel"], f"{name} references '{ref}', which no asmdef or package provides"))
                    continue
                if pkg not in manifest and pkg not in TRANSITIVE_PACKAGES:
                    findings.append(Finding("ERROR", "unknown-reference", d["rel"], f"{name} references '{ref}' from {pkg}, which manifest.json does not pin"))
            if editor_only(ref) and not editor_only(name) and ref != "UnityEditor.TestRunner":
                findings.append(Finding("ERROR", "player-refs-editor", d["rel"], f"{name} builds for the player but references Editor-only {ref}"))
        if name.endswith("Tests"):
            missing = []
            if "nunit.framework.dll" not in d.get("precompiledReferences", []):
                missing.append("precompiledReferences nunit.framework.dll")
            if "UnityEngine.TestRunner" not in refs:
                missing.append("reference UnityEngine.TestRunner")
            if "UNITY_INCLUDE_TESTS" not in d.get("defineConstraints", []):
                missing.append("defineConstraints UNITY_INCLUDE_TESTS")
            if missing:
                findings.append(Finding("ERROR", "test-asmdef", d["rel"], f"{name} lacks " + ", ".join(missing)))

    state: dict[str, int] = {}

    def visit(n: str, path: list[str]) -> None:
        state[n] = 1
        for r in asmdefs[n].get("references", []):
            if r not in asmdefs:
                continue
            if state.get(r) == 1:
                cycle = path[path.index(r):] + [r] if r in path else [n, r]
                findings.append(Finding("ERROR", "cycle", asmdefs[n]["rel"], " -> ".join(cycle)))
            elif r not in state:
                visit(r, path + [r])
        state[n] = 2

    for n in asmdefs:
        if n not in state:
            visit(n, [n])

    def owner(cs: Path) -> str | None:
        best = None
        for n, d in asmdefs.items():
            if d["dir"] == cs.parent or d["dir"] in cs.parents:
                if best is None or len(d["dir"].parts) > len(asmdefs[best]["dir"].parts):
                    best = n
        return best

    files: dict[str, tuple[str, str, str]] = {}
    namespace_providers: dict[str, set[str]] = defaultdict(set)
    types: dict[str, list[tuple[str, str]]] = defaultdict(list)
    bases: dict[str, tuple[str, str]] = {}
    declaration = re.compile(r"\b(?:class|struct|interface|enum)\s+(\w+)(?:\s*<[^>{]*>)?(?:\s*:\s*([\w.<>, ]+?))?\s*(?:where\b|\{)")
    for cs in sorted(game.rglob("*.cs")):
        rel = cs.relative_to(root).as_posix()
        asm = owner(cs)
        if asm is None:
            continue  # check_rules.py reports scripts outside every asmdef
        raw = cs.read_text(encoding="utf-8-sig", errors="replace")
        text = strip_comments_and_strings(raw)
        namespaces = re.findall(r"\bnamespace\s+([\w.]+)", text)
        for ns in namespaces:
            parts = ns.split(".")
            for i in range(1, len(parts) + 1):
                namespace_providers[".".join(parts[:i])].add(asm)
        ns = namespaces[0] if namespaces else ""
        for m in declaration.finditer(text):
            types[(ns + "." if ns else "") + m.group(1)].append((asm, rel))
            if m.group(2):
                bases[m.group(1)] = (m.group(2).split(",")[0].strip().split("<")[0].split(".")[-1], rel)
        files[rel] = (asm, text, raw)

    for full, where in sorted(types.items()):
        if len(where) > 1:
            findings.append(Finding("ERROR", "duplicate-type", where[1][1], f"{full} is also declared in {where[0][1]}"))

    def unity_object_kind(cls: str, depth: int = 0) -> str | None:
        if depth > 12 or cls not in bases:
            return None
        base = bases[cls][0]
        return base if base in UNITY_OBJECT_BASES else unity_object_kind(base, depth + 1)

    for cls, (_, rel) in sorted(bases.items()):
        kind = unity_object_kind(cls)
        if kind and Path(rel).stem != cls:
            findings.append(Finding("ERROR", "unity-object-file-name", rel,
                                    f"{kind} {cls} must live in {cls}.cs, or Unity loses the script reference when the scene is saved"))

    for rel, (asm, text, raw) in files.items():
        d = asmdefs[asm]
        visible = set(d.get("references", [])) | {asm} | set(d.get("precompiledReferences", []))
        for m in re.finditer(r"^\s*using\s+(?:static\s+)?(?:\w+\s*=\s*)?([\w.]+)\s*;", text, flags=re.M):
            used = m.group(1)
            line = text.count("\n", 0, m.start()) + 1
            if used.startswith("PatientZero"):
                providers = namespace_providers.get(used, set())
                if not providers & visible:
                    findings.append(Finding("ERROR", "using-without-reference", f"{rel}:{line}",
                                            f"using {used}; is provided by {sorted(providers) or 'no assembly'}, and {asm} references none of them"))
                continue
            for prefix, needed in NAMESPACE_ASSEMBLIES:
                if used == prefix or used.startswith(prefix + "."):
                    if needed not in visible:
                        findings.append(Finding("ERROR", "using-without-reference", f"{rel}:{line}", f"using {used}; needs {needed}, which {asm} does not reference"))
                    break
            else:
                if not used.startswith(ENGINE_NAMESPACES):
                    findings.append(Finding("WARN", "using-unknown-namespace", f"{rel}:{line}", f"using {used};"))
        if not editor_only(asm):
            blocks: list[bool] = []
            for i, line in enumerate(raw.splitlines(), 1):
                s = line.strip()
                if s.startswith("#if"):
                    blocks.append("UNITY_EDITOR" in s and "!UNITY_EDITOR" not in s)
                elif s.startswith("#else") and blocks:
                    blocks[-1] = not blocks[-1]
                elif s.startswith("#endif") and blocks:
                    blocks.pop()
                elif re.search(r"\bUnityEditor\b", strip_comments_and_strings(line)) and not any(blocks):
                    findings.append(Finding("ERROR", "editor-api-in-player", f"{rel}:{i}",
                                            f"{asm} ships in the player; wrap UnityEditor use in #if UNITY_EDITOR or move it to an Editor assembly"))

    runtime = {rel: v for rel, v in files.items() if "/Tests/" not in rel}
    registered: dict[str, list[str]] = defaultdict(list)
    read: dict[str, set[str]] = defaultdict(set)
    raised: dict[str, set[str]] = defaultdict(set)
    heard: dict[str, set[str]] = defaultdict(set)
    for rel, (asm, text, raw) in runtime.items():
        area = rel.split("PatientZero/")[1].split("/")[0]
        for m in re.finditer(r"Services<\s*(\w+)\s*>\.Register\s*\(", text):
            registered[m.group(1)].append(Path(rel).stem)
        for m in re.finditer(r"Services<\s*(\w+)\s*>\.(?:TryGet|Get)\b", text):
            read[m.group(1)].add(rel)
        for m in re.finditer(r"GameEvents\.(\w+)\.Raise\s*\(", text):
            raised[m.group(1)].add(area)
        for m in re.finditer(r"GameEvents\.(\w+)\.Subscribe\s*\(", text):
            heard[m.group(1)].add(area)
    for iface, users in sorted(read.items()):
        if iface not in registered:
            findings.append(Finding("ERROR", "service-unregistered", sorted(users)[0], f"Services<{iface}> is read but no runtime code registers it"))

    events_file = game / "Core/Contracts/GameEvents.cs"
    channels = re.findall(r"EventChannel<\w+>\s+(\w+)\s*=", events_file.read_text(encoding="utf-8")) if events_file.exists() else []
    for c in channels:
        if heard[c] and not raised[c]:
            findings.append(Finding("WARN", "event-no-writer", c, f"heard by {', '.join(sorted(heard[c]))}; nothing raises it"))
        if raised[c] and not heard[c]:
            findings.append(Finding("WARN", "event-no-reader", c, f"raised by {', '.join(sorted(raised[c]))}; nothing subscribes"))

    if tables is not None:
        tables["events"] = {c: (sorted(raised[c]), sorted(heard[c])) for c in channels}
        tables["services"] = {k: sorted(v) for k, v in registered.items()}
        tables["counts"] = (len(asmdefs), len(files), len(types))
    return findings


def write(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def self_test() -> int:
    """Plant one defect per rule in a scratch tree and confirm each is reported; a clean tree must pass."""
    with tempfile.TemporaryDirectory() as tmp:
        root = Path(tmp)
        g = root / PZ
        write(root / "Game/Packages/manifest.json", json.dumps({"dependencies": {"com.unity.xr.interaction.toolkit": "3.7.0", "com.unity.test-framework": "1.6.0"}}))
        write(g / "Core/PatientZero.Core.asmdef", json.dumps({"name": "PatientZero.Core", "references": []}))
        write(g / "Core/Contracts/GameEvents.cs",
              "namespace PatientZero.Core { public static class GameEvents { public static readonly EventChannel<int> Ping = null;"
              " public static readonly EventChannel<int> Pong = null; } }")
        write(g / "Core/Contracts/Services.cs", "namespace PatientZero.Core { public static class Services<T> { } public interface IThing { } }")
        write(g / "UI/Runtime/PatientZero.UI.asmdef", json.dumps({"name": "PatientZero.UI", "references": ["PatientZero.Core", "UnityEngine.UI"]}))
        write(g / "UI/Runtime/Panel.cs", "using PatientZero.Core;\nusing UnityEngine;\nusing UnityEngine.UI;\nnamespace PatientZero.UI { public sealed class Panel : MonoBehaviour {"
              " void OnEnable(){ GameEvents.Ping.Subscribe(null); GameEvents.Pong.Raise(0); GameEvents.Pong.Subscribe(null); GameEvents.Ping.Raise(0); Services<IThing>.Register(this); } } }")
        write(g / "UI/Runtime/Reader.cs", "using PatientZero.Core;\nnamespace PatientZero.UI { public sealed class Reader { void R(){ Services<IThing>.TryGet(out var t); } } }")
        write(g / "UI/Tests/EditMode/PatientZero.UI.Tests.asmdef", json.dumps({
            "name": "PatientZero.UI.Tests", "references": ["PatientZero.UI", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
            "includePlatforms": ["Editor"], "precompiledReferences": ["nunit.framework.dll"], "defineConstraints": ["UNITY_INCLUDE_TESTS"]}))
        write(g / "UI/Tests/EditMode/PanelTests.cs", "using NUnit.Framework;\nusing PatientZero.UI;\nnamespace PatientZero.UI.Tests { public class PanelTests { } }")
        clean = [f for f in scan(root) if f.severity == "ERROR" or f.rule.startswith("event-")]
        if clean:
            print("SELF-TEST FAILED: clean tree reported:\n  " + "\n  ".join(map(str, clean)))
            return 1

        write(g / "AI/Runtime/PatientZero.AI.asmdef", json.dumps({"name": "PatientZero.AI", "references": ["PatientZero.Core", "Unity.ugui", "PatientZero.AI.Editor"]}))
        write(g / "AI/Editor/PatientZero.AI.Editor.asmdef", json.dumps({"name": "PatientZero.AI.Editor", "references": ["PatientZero.AI"], "includePlatforms": ["Editor"]}))
        write(g / "AI/Runtime/Brain.cs", "using PatientZero.Core;\nusing UnityEngine;\nusing UnityEngine.UI;\nusing PatientZero.Flow;\nusing UnityEditor;\n"
              "namespace PatientZero.AI { public class Walker : MonoBehaviour { void A(){ UnityEditor.EditorApplication.Beep(); GameEvents.Gone.Subscribe(null);"
              " Services<IMissing>.TryGet(out var m); } } }")
        write(g / "AI/Runtime/Panel.cs", "namespace PatientZero.UI { public class Panel { } }")
        write(g / "AI/Runtime/Guarded.cs", "namespace PatientZero.AI { public class Guarded {\n#if UNITY_EDITOR\nvoid G(){ UnityEditor.EditorApplication.Beep(); }\n#endif\n} }")
        write(g / "AI/Tests/EditMode/PatientZero.AI.Tests.asmdef", json.dumps({"name": "PatientZero.AI.Tests", "references": ["PatientZero.AI"], "optionalUnityReferences": ["TestAssemblies"]}))
        write(g / "Core/Contracts/GameEvents.cs",
              "namespace PatientZero.Core { public static class GameEvents { public static readonly EventChannel<int> Ping = null;"
              " public static readonly EventChannel<int> Pong = null; public static readonly EventChannel<int> Gone = null; } }")
        got = {(f.rule, Path(f.where.split(":")[0]).name if "/" in f.where else f.where) for f in scan(root)}
        expected = {
            ("unknown-reference", "PatientZero.AI.asmdef"), ("player-refs-editor", "PatientZero.AI.asmdef"), ("cycle", "PatientZero.AI.asmdef"),
            ("test-asmdef", "PatientZero.AI.Tests.asmdef"), ("duplicate-type", "Panel.cs"), ("unity-object-file-name", "Brain.cs"),
            ("using-without-reference", "Brain.cs"), ("editor-api-in-player", "Brain.cs"), ("service-unregistered", "Brain.cs"),
            ("event-no-writer", "Gone"),
        }
        missed = expected - got
        guarded = {g_ for g_ in got if g_ == ("editor-api-in-player", "Guarded.cs")}
        if missed or guarded:
            print("SELF-TEST FAILED: missed " + str(sorted(missed)) + ("; flagged code inside #if UNITY_EDITOR" if guarded else ""))
            return 1
    print("SELF-TEST PASSED")
    return 0


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--root", type=Path, default=REPO)
    parser.add_argument("--wiring", action="store_true", help="print the event and service wiring tables")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args(argv)
    if args.self_test:
        return self_test()
    tables: dict = {}
    findings = scan(args.root, tables)
    for f in findings:
        print(f)
    if args.wiring:
        print("\nEvents (raised by -> heard by):")
        for c, (r, h) in tables["events"].items():
            print(f"  {c:20s} {', '.join(r) or '-'} -> {', '.join(h) or '-'}")
        print("Services (registered by):")
        for s, who in sorted(tables["services"].items()):
            print(f"  {s:20s} {', '.join(who)}")
    errors = sum(f.severity == "ERROR" for f in findings)
    warns = sum(f.severity == "WARN" for f in findings)
    a, n, t = tables["counts"]
    print(f"{a} assemblies, {n} scripts, {t} types: {errors} error(s), {warns} warning(s).")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
