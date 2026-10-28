"""Static review checks for PATIENT ZERO branches.

Runs without Unity. It catches contract breaches early; it does not replace compiling in
the Unity Editor or testing on a headset.

  python tools/review/check_rules.py                       # code rules on the working tree
  python tools/review/check_rules.py --seat A --base main --head codex/a-player-interaction
  python tools/review/check_rules.py --self-test           # prove each rule fires

Exit code 1 if any ERROR. Rules are documented in docs/REVIEW_CHECKLIST.md.
"""

from __future__ import annotations

import argparse
import fnmatch
import json
import re
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PZ = "Game/Assets/PatientZero"

SEAT_A_AREAS = ("Player", "Interaction", "Flow", "UI")
SEAT_B_AREAS = ("AI", "Audio", "Atmosphere", "Environment")

OWNERSHIP = {
    "A": [f"{PZ}/{a}/*" for a in SEAT_A_AREAS] + ["docs/collab/CODEX_A_LOG.md"],
    "B": [f"{PZ}/{a}/*" for a in SEAT_B_AREAS] + ["docs/collab/CODEX_B_LOG.md"],
    "claude": [
        f"{PZ}/Core/*", f"{PZ}/Integration/*", "Game/Packages/*", "Game/ProjectSettings/*",
        "tools/*", "docs/*", "README.md", ".gitignore", ".gitattributes", "*.meta",
    ],
}

# Which area may Raise each GameEvents channel ("*" = anyone). Tests are exempt.
EVENT_WRITERS = {
    "StateChanged": {"Flow"},
    "StateRequested": {"*"},
    "ActStarted": {"Atmosphere"},
    "ObjectiveProgress": {"Interaction"},
    "ObjectiveCompleted": {"Interaction"},
    "AssistChanged": {"Atmosphere"},
    "Noise": {"Audio"},
    "ZombieAlert": {"AI"},
    "PlayerCaught": {"AI"},
    "LightingChanged": {"Atmosphere"},
    "InfectionReveal": {"Atmosphere"},
    "CaptionRequested": {"Audio"},
    "SettingsChanged": {"Flow"},
    "TrackingChanged": {"Player"},
    "HapticRequested": {"AI", "Atmosphere"},
}

UNITY_YAML = (".unity", ".prefab", ".asset", ".mat", ".controller", ".anim", ".meta",
              ".physicMaterial", ".mixer", ".lighting", ".overrideController", ".playable")

HOT_METHODS = ("Update", "LateUpdate", "FixedUpdate", "OnTriggerStay", "OnCollisionStay",
               "OnAudioFilterRead", "OnAnimatorMove", "OnAnimatorIK")


@dataclass
class Finding:
    severity: str
    rule: str
    path: str
    line: int
    message: str

    def __str__(self) -> str:
        where = f"{self.path}:{self.line}" if self.line else self.path
        return f"{self.severity:5} {self.rule:16} {where}  {self.message}"


def blank_comments_and_strings(source: str) -> str:
    """Replace comment text and string contents with spaces, keeping quotes, $ and newlines,
    so regexes neither match inside comments nor lose line numbers."""
    out = []
    i, n = 0, len(source)
    while i < n:
        c = source[i]
        nxt = source[i + 1] if i + 1 < n else ""
        if c == "/" and nxt == "/":
            while i < n and source[i] != "\n":
                out.append(" ")
                i += 1
        elif c == "/" and nxt == "*":
            out.append("  ")
            i += 2
            while i < n and not (source[i] == "*" and i + 1 < n and source[i + 1] == "/"):
                out.append("\n" if source[i] == "\n" else " ")
                i += 1
            out.append("  ")
            i += 2
        elif c == '"' or (c == "@" and nxt == '"'):
            verbatim = c == "@"
            if verbatim:
                out.append("@")
                i += 1
            out.append('"')
            i += 1
            while i < n:
                ch = source[i]
                if not verbatim and ch == "\\" and i + 1 < n:
                    out.append("  ")
                    i += 2
                    continue
                if ch == '"':
                    if verbatim and i + 1 < n and source[i + 1] == '"':
                        out.append("  ")
                        i += 2
                        continue
                    out.append('"')
                    i += 1
                    break
                out.append("\n" if ch == "\n" else " ")
                i += 1
        elif c == "'" and i + 2 < n:
            end = source.find("'", i + 1 + (1 if nxt == "\\" else 0))
            if end != -1 and end - i <= 8:
                out.append("'" + " " * (end - i - 1) + "'")
                i = end + 1
            else:
                out.append(c)
                i += 1
        else:
            out.append(c)
            i += 1
    return "".join(out)


def line_of(text: str, index: int) -> int:
    return text.count("\n", 0, index) + 1


def area_of(rel: str) -> str | None:
    parts = rel.split("/")
    if rel.startswith(PZ + "/") and len(parts) > 3:
        return parts[3]
    return None


def is_test_path(rel: str) -> bool:
    return "/Tests/" in rel


def is_editor_path(rel: str) -> bool:
    return "/Editor/" in rel


def is_logic_path(rel: str) -> bool:
    return "/Logic/" in rel


def find_bodies(clean: str, names: tuple[str, ...]):
    pattern = re.compile(r"\bvoid\s+(" + "|".join(names) + r")\s*\([^)]*\)\s*\{")
    for m in pattern.finditer(clean):
        depth, i = 1, m.end()
        while i < len(clean) and depth:
            if clean[i] == "{":
                depth += 1
            elif clean[i] == "}":
                depth -= 1
            i += 1
        yield m.group(1), m.end(), i


CODE_RULES = [
    # (rule id, severity, regex, applies(rel) -> bool, message)
    ("audio-owner", "ERROR", r"\bAudioSource\b|PlayClipAtPoint|\bAudioMixer\b",
     lambda r: area_of(r) not in ("Audio",) and not is_test_path(r),
     "Only PatientZero.Audio touches AudioSource/AudioMixer. Play sounds with Cues.Play(CueId, ...)."),
    ("light-owner", "ERROR", r"\bLight\b|\bRenderSettings\b|\bLightmapSettings\b",
     lambda r: area_of(r) not in ("Atmosphere", "Environment") and not is_test_path(r),
     "Only Atmosphere/Environment touch lights, fog or ambient. Raise a request via the director."),
    ("pause-owner", "ERROR", r"Time\.timeScale\s*=(?!=)|AudioListener\.pause\s*=(?!=)",
     lambda r: area_of(r) != "Flow" and not is_test_path(r),
     "Only GameFlow writes Time.timeScale or AudioListener.pause."),
    ("unscaled-time", "WARN", r"Time\.unscaled(Delta)?Time",
     lambda r: area_of(r) not in ("Flow", "UI", "Player") and not is_test_path(r),
     "Unscaled time keeps running while paused. Gameplay must use scaled time."),
    ("wrong-sdk", "ERROR", r"\bOVR[A-Z]\w*|\bOculus\.|\busing\s+Oculus\b|\bMeta\.XR\.(?!Audio)",
     lambda r: True,
     "The project uses Unity OpenXR + XR Interaction Toolkit, not the Meta/Oculus integration SDK."),
    ("b-no-xri", "ERROR", r"UnityEngine\.XR\.Interaction\.Toolkit|UnityEngine\.XR\.Hands",
     lambda r: area_of(r) in SEAT_B_AREAS and not is_test_path(r),
     "Seat B code may not depend on the interaction stack; use IPlayerQuery from Core."),
    ("scene-search", "ERROR",
     r"\bFindObjectOfType\b|\bFindObjectsOfType\b|\bFindFirstObjectByType\b|\bFindAnyObjectByType\b|"
     r"\bFindObjectsByType\b|GameObject\.Find\b|\bFindWithTag\b|\bFindGameObjectsWithTag\b|"
     r"\bSendMessage\b|\bBroadcastMessage\b",
     lambda r: not is_editor_path(r) and not is_test_path(r),
     "No scene searches or SendMessage at runtime. Use Services<T> or serialized references."),
    ("linq-runtime", "ERROR", r"\busing\s+System\.Linq\b",
     lambda r: not is_editor_path(r) and not is_test_path(r),
     "No LINQ in runtime code (hidden allocations on Quest)."),
    ("string-invoke", "WARN", r"\bInvoke\s*\(\s*\"|\bInvokeRepeating\b",
     lambda r: not is_test_path(r),
     "String-based Invoke breaks silently on rename; use a timer or coroutine."),
    ("async-void", "WARN", r"\basync\s+void\b",
     lambda r: not is_test_path(r),
     "async void swallows exceptions and outlives scene reloads."),
    ("resources-load", "WARN", r"\bResources\.Load",
     lambda r: not is_test_path(r),
     "Prefer serialized references; Resources.Load hides dependencies from review."),
    ("debug-log", "WARN", r"\bDebug\.Log\s*\(",
     lambda r: not is_editor_path(r) and not is_test_path(r) and area_of(r) != "Core",
     "Unconditional Debug.Log allocates and spams logcat on device; guard or remove."),
    ("cross-seat-ns", "ERROR", r"\bPatientZero\.(AI|Audio|Atmosphere|Environment)\b",
     lambda r: area_of(r) in SEAT_A_AREAS and not is_test_path(r),
     "Seat A code may not use seat B namespaces; talk through PatientZero.Core."),
    ("cross-seat-ns", "ERROR", r"\bPatientZero\.(Player|Interaction|Flow|UI)\b",
     lambda r: area_of(r) in SEAT_B_AREAS and not is_test_path(r),
     "Seat B code may not use seat A namespaces; talk through PatientZero.Core."),
    ("logic-engine", "ERROR",
     r"\bMonoBehaviour\b|\bTime\.|\bUnityEngine\.Random\b|\bRandom\.(Range|value)\b|\bPhysics\.|\bGameObject\b|\bResources\.",
     lambda r: is_logic_path(r) and not is_test_path(r),
     "Logic/ code is engine-free: pass time, randomness and scene data in from the MonoBehaviour shell."),
]

HOT_RULES = [
    ("hot-alloc", "ERROR", r"\bnew\s+(List|Dictionary|HashSet|Queue|Stack|LinkedList|StringBuilder)\s*<|\bnew\s+\w+\s*\[",
     "Allocates every frame."),
    ("hot-string", "ERROR", r"\$\"|\"\s*\+|\+\s*\"|\.ToString\s*\(|string\.Format|string\.Concat",
     "String building allocates every frame."),
    ("hot-lookup", "ERROR", r"\bGetComponent(s|InChildren|InParent)?\s*[<(]|\bCamera\.main\b",
     "Cache component and camera lookups in Awake."),
    ("hot-linq", "ERROR", r"\.(Where|Select|Any|All|First|FirstOrDefault|ToList|ToArray|OrderBy|Count)\s*\(",
     "LINQ in a per-frame method allocates."),
    ("hot-closure", "WARN", r"=>",
     "A lambda in a per-frame method can allocate a closure."),
    ("hot-coroutine", "WARN", r"\bStartCoroutine\b",
     "Starting coroutines per frame allocates."),
]


def check_code(root: Path, findings: list[Finding]) -> None:
    base = root / PZ
    if not base.exists():
        return
    for path in sorted(base.rglob("*.cs")):
        rel = path.relative_to(root).as_posix()
        clean = blank_comments_and_strings(path.read_text(encoding="utf-8", errors="replace"))

        for rule, severity, pattern, applies, message in CODE_RULES:
            if not applies(rel):
                continue
            for m in re.finditer(pattern, clean):
                findings.append(Finding(severity, rule, rel, line_of(clean, m.start()), message))

        if is_logic_path(rel) and not is_test_path(rel):
            own = area_of(rel)
            for m in re.finditer(r"\bPatientZero\.(\w+)", clean):
                if m.group(1) not in ("Core", own):
                    findings.append(Finding("ERROR", "logic-deps", rel, line_of(clean, m.start()),
                                            f"{own}/Logic may use only PatientZero.Core and its own area, not PatientZero.{m.group(1)}."))

        if not is_test_path(rel):
            area = area_of(rel)
            for m in re.finditer(r"GameEvents\.(\w+)\.Raise\s*\(", clean):
                channel = m.group(1)
                writers = EVENT_WRITERS.get(channel)
                if writers is None:
                    findings.append(Finding("ERROR", "event-unknown", rel, line_of(clean, m.start()),
                                            f"GameEvents.{channel} is not in the writer table."))
                elif "*" not in writers and area not in writers:
                    findings.append(Finding("ERROR", "event-writer", rel, line_of(clean, m.start()),
                                            f"{area} may not raise GameEvents.{channel} (writers: {', '.join(sorted(writers))})."))

        if not is_editor_path(rel) and not is_test_path(rel):
            for method, start, end in find_bodies(clean, HOT_METHODS):
                body = clean[start:end]
                for rule, severity, pattern, message in HOT_RULES:
                    for m in re.finditer(pattern, body):
                        findings.append(Finding(severity, rule, rel, line_of(clean, start + m.start()),
                                                f"In {method}(): {message}"))


def check_asmdefs(root: Path, findings: list[Finding]) -> None:
    base = root / PZ
    if not base.exists():
        return
    asmdef_dirs = set()
    for path in sorted(base.rglob("*.asmdef")):
        rel = path.relative_to(root).as_posix()
        asmdef_dirs.add(path.parent)
        try:
            data = json.loads(path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as e:
            findings.append(Finding("ERROR", "asmdef-json", rel, 0, f"Invalid JSON: {e}"))
            continue
        name = data.get("name", "")
        refs = [r for r in data.get("references", []) if not r.startswith("GUID:")]
        if any(r.startswith("GUID:") for r in data.get("references", [])):
            findings.append(Finding("WARN", "asmdef-guid", rel, 0, "Reference asmdefs by name, not GUID, so branches without .meta files resolve."))
        seat = seat_of_assembly(name)
        is_test = name.endswith(".Tests")
        for ref in refs:
            ref_seat = seat_of_assembly(ref)
            if name == "PatientZero.Core" and ref.startswith("PatientZero."):
                findings.append(Finding("ERROR", "asmdef-core", rel, 0, "PatientZero.Core must reference nothing."))
            elif name.endswith(".Logic") and ref.startswith("PatientZero.") and ref != "PatientZero.Core":
                findings.append(Finding("ERROR", "asmdef-logic", rel, 0, f"{name} may reference only PatientZero.Core, not {ref}."))
            elif seat in ("A", "B") and ref_seat in ("A", "B") and ref_seat != seat and not is_test:
                findings.append(Finding("ERROR", "asmdef-cross", rel, 0, f"{name} (seat {seat}) references {ref} (seat {ref_seat}); talk through Core."))
            elif seat == "B" and ref in ("Unity.XR.Interaction.Toolkit", "Unity.XR.Hands"):
                findings.append(Finding("ERROR", "asmdef-b-xri", rel, 0, f"{name} may not reference {ref}."))
        if name.endswith(".Logic") and data.get("references") and "PatientZero.Core" not in refs:
            findings.append(Finding("WARN", "asmdef-logic", rel, 0, "Logic assemblies normally reference PatientZero.Core."))
        if not name.startswith("PatientZero."):
            findings.append(Finding("WARN", "asmdef-name", rel, 0, f"Assembly '{name}' should be named PatientZero.<Area>[.Logic|.Editor|.Tests]."))

    for path in sorted(base.rglob("*.cs")):
        rel = path.relative_to(root).as_posix()
        if not any(d == path.parent or d in path.parents for d in asmdef_dirs):
            findings.append(Finding("ERROR", "no-asmdef", rel, 0, "Script is outside every asmdef and would compile into Assembly-CSharp."))


def seat_of_assembly(name: str) -> str | None:
    parts = name.split(".")
    if len(parts) < 2 or parts[0] != "PatientZero":
        return None
    if parts[1] in SEAT_A_AREAS:
        return "A"
    if parts[1] in SEAT_B_AREAS:
        return "B"
    if parts[1] in ("Core", "Integration"):
        return "claude"
    return None


def git(root: Path, *args: str) -> str:
    result = subprocess.run(["git", "-C", str(root), *args], capture_output=True, text=True)
    if result.returncode != 0:
        raise SystemExit(f"git {' '.join(args)} failed: {result.stderr.strip()}")
    return result.stdout


def check_diff(root: Path, seat: str, base: str, head: str, allow_yaml: bool, findings: list[Finding]) -> None:
    changed = [l for l in git(root, "diff", "--name-only", f"{base}...{head}").splitlines() if l]
    owned = OWNERSHIP[seat]
    for rel in changed:
        if not any(fnmatch.fnmatch(rel, g) for g in owned):
            severity = "WARN" if seat == "claude" else "ERROR"
            findings.append(Finding(severity, "ownership", rel, 0, f"Seat {seat} does not own this path."))
        if not allow_yaml and rel.endswith(UNITY_YAML) and seat != "claude":
            findings.append(Finding("ERROR", "hand-yaml", rel, 0,
                                    "Unity YAML/.meta must be generated by the Editor (scene build steps), not hand-written."))
    check_enum_append_only(root, base, head, findings)


ENUM_RE = re.compile(r"\benum\s+(\w+)\s*\{([^}]*)\}", re.S)
MEMBER_RE = re.compile(r"(\w+)\s*=\s*(-?\d+)")


def parse_enums(text: str) -> dict[str, dict[str, int]]:
    clean = blank_comments_and_strings(text)
    return {m.group(1): {k: int(v) for k, v in MEMBER_RE.findall(m.group(2))} for m in ENUM_RE.finditer(clean)}


def check_enum_append_only(root: Path, base: str, head: str, findings: list[Finding]) -> None:
    rel = f"{PZ}/Core/Contracts/Ids.cs"
    def show(ref: str) -> str:
        r = subprocess.run(["git", "-C", str(root), "show", f"{ref}:{rel}"], capture_output=True, text=True)
        return r.stdout if r.returncode == 0 else ""
    before, after = parse_enums(show(base)), parse_enums(show(head))
    for enum, members in before.items():
        for member, value in members.items():
            now = after.get(enum, {}).get(member)
            if now is None:
                findings.append(Finding("ERROR", "enum-append", rel, 0, f"{enum}.{member} was removed or renamed."))
            elif now != value:
                findings.append(Finding("ERROR", "enum-append", rel, 0, f"{enum}.{member} changed {value} -> {now}."))


def run(root: Path, seat: str | None, base: str | None, head: str | None, allow_yaml: bool) -> list[Finding]:
    findings: list[Finding] = []
    check_code(root, findings)
    check_asmdefs(root, findings)
    if seat and base and head:
        check_diff(root, seat, base, head, allow_yaml, findings)
    return findings


def self_test() -> int:
    """Builds a throwaway repo with one violation per rule and checks each one is reported."""
    bad_a = """using System.Linq;
using UnityEngine;
using PatientZero.Core;
public class Bad : MonoBehaviour {
    // AudioSource in a comment must NOT be reported
    AudioSource src;
    void Update() {
        var l = new List<int>();
        var s = "x" + name;
        var c = GetComponent<Rigidbody>();
        var d = Camera.main;
        GameEvents.Noise.Raise(default(NoiseEvent));
    }
    void Start() { Time.timeScale = 0f; FindObjectOfType<Bad>(); OVRInput.Get(); string ok = "Light FindObjectOfType"; }
}"""
    bad_logic = "using UnityEngine; using PatientZero.Flow.Logic; public class L { float t = Time.time; }"
    bad_b = "using UnityEngine.XR.Interaction.Toolkit; using PatientZero.Interaction; public class Z { }"
    expected = {"linq-runtime", "audio-owner", "hot-alloc", "hot-string", "hot-lookup", "event-writer",
                "pause-owner", "scene-search", "wrong-sdk", "logic-engine", "b-no-xri", "no-asmdef",
                "asmdef-cross", "ownership", "hand-yaml", "enum-append", "cross-seat-ns", "logic-deps"}
    with tempfile.TemporaryDirectory() as tmp:
        root = Path(tmp)
        def write(rel: str, text: str) -> None:
            p = root / rel
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_text(text, encoding="utf-8")
        write(f"{PZ}/Core/Contracts/Ids.cs", "namespace X { public enum CueId { None = 0, A = 1 } }")
        write(f"{PZ}/Interaction/PatientZero.Interaction.asmdef", json.dumps({"name": "PatientZero.Interaction", "references": ["PatientZero.Core", "PatientZero.AI"]}))
        subprocess.run(["git", "init", "-q", "-b", "main", str(root)], check=True)
        subprocess.run(["git", "-C", str(root), "add", "-A"], check=True)
        subprocess.run(["git", "-C", str(root), "-c", "user.name=t", "-c", "user.email=t@t", "commit", "-qm", "base"], check=True)
        write(f"{PZ}/Interaction/Bad.cs", bad_a)
        write(f"{PZ}/Interaction/Logic/L.cs", bad_logic)
        write(f"{PZ}/AI/Z.cs", bad_b)
        write(f"{PZ}/AI/Ward.unity", "%YAML 1.1")
        write(f"{PZ}/Core/Contracts/Ids.cs", "namespace X { public enum CueId { None = 0, A = 2 } }")
        subprocess.run(["git", "-C", str(root), "add", "-A"], check=True)
        subprocess.run(["git", "-C", str(root), "-c", "user.name=t", "-c", "user.email=t@t", "commit", "-qm", "head"], check=True)
        found = run(root, "A", "HEAD~1", "HEAD", allow_yaml=False)

    rules = {f.rule for f in found}
    missing = expected - rules
    comment_hits = [f for f in found if f.rule == "audio-owner" and f.line == 5]
    string_hits = [f for f in found if f.line == 14 and f.rule in ("light-owner",)]
    ok = not missing and not comment_hits and not string_hits
    for f in found:
        print(f)
    print()
    print("missing rules:", sorted(missing) or "none")
    print("comment false positives:", len(comment_hits), " string false positives:", len(string_hits))
    print("SELF-TEST", "PASSED" if ok else "FAILED")
    return 0 if ok else 1


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--seat", choices=["A", "B", "claude"])
    parser.add_argument("--base")
    parser.add_argument("--head")
    parser.add_argument("--allow-yaml", action="store_true", help="Allow Unity YAML changes (only once agents run the Editor).")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        return self_test()
    findings = run(ROOT, args.seat, args.base, args.head, args.allow_yaml)
    for f in sorted(findings, key=lambda f: (f.severity != "ERROR", f.path, f.line)):
        print(f)
    errors = sum(f.severity == "ERROR" for f in findings)
    warns = sum(f.severity == "WARN" for f in findings)
    print(f"\n{errors} error(s), {warns} warning(s).")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
