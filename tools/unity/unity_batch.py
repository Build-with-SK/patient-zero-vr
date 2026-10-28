#!/usr/bin/env python3
"""Run the PatientZero editor tooling in Unity batchmode with the pinned editor, and read the results back.

  python tools/unity/unity_batch.py open           # open the project: resolve packages (writes packages-lock.json), compile
  python tools/unity/unity_batch.py import-samples # import the XRI/XR Hands samples the scene build steps use
  python tools/unity/unity_batch.py compose        # build Bootstrap.unity + Ward.unity, validate layout and services
  python tools/unity/unity_batch.py validate       # editor, packages, URP, Android, XR loader, OpenXR features
  python tools/unity/unity_batch.py configure      # apply the Quest configuration (writes ProjectSettings and Assets/XR)
  python tools/unity/unity_batch.py desktop-sim    # import the XRI simulator samples and switch to desktop simulation
  python tools/unity/unity_batch.py build-apk      # configure + compose + validate + Builds/Android/PatientZero-dev.apk
  python tools/unity/unity_batch.py editmode-tests # every EditMode test assembly, NUnit XML in Game/Logs/PatientZero
  python tools/unity/unity_batch.py playmode-tests # every PlayMode test assembly, NUnit XML in Game/Logs/PatientZero
  python tools/unity/unity_batch.py first-run      # all of the above in order; summary in Game/Logs/PatientZero/first-run.json

The editor is found from $PZ_UNITY, else the Unity Hub install folder for the pinned version.
Exit codes: 0 pass, 1 Unity ran and reported failure, 2 BLOCKED (no pinned editor, or a step needs a missing module).
"""
from __future__ import annotations

import argparse
import json
import os
import platform
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
PROJECT = REPO / "Game"
LOGS = PROJECT / "Logs" / "PatientZero"
BLOCKED = 2

# command -> (execute method or None, build target or None)
COMMANDS = {
    "open": (None, None),
    "import-samples": ("PatientZero.Integration.Editor.PackageSamples.ImportRequiredBatch", None),
    "compose": ("PatientZero.Integration.Editor.SceneComposer.BuildAllBatch", None),
    "validate": ("PatientZero.Integration.Editor.Quest.QuestBuild.ValidateBatch", None),
    "configure": ("PatientZero.Integration.Editor.Quest.QuestBuild.ConfigureBatch", "Android"),
    "desktop-sim": ("PatientZero.Integration.Editor.Quest.DesktopSimulation.EnableBatch", None),
    "build-apk": ("PatientZero.Integration.Editor.Quest.QuestBuild.BuildDevApkBatch", "Android"),
    "editmode-tests": (None, None),
    "playmode-tests": (None, None),
}
TEST_PLATFORMS = {"editmode-tests": "EditMode", "playmode-tests": "PlayMode"}

# first-run order. Each step runs in its own editor session: samples must compile before compose uses them.
# validate runs after build-apk because it checks the Android configuration that configure writes.
FIRST_RUN = ["open", "import-samples", "compose", "editmode-tests", "playmode-tests", "desktop-sim", "build-apk", "validate"]
NEEDS_ANDROID = {"configure", "build-apk", "validate"}


def pinned_editor(root: Path = REPO) -> str:
    return json.loads((root / "tools/unity/package_policy.json").read_text(encoding="utf-8"))["editor"]


def candidate_paths(version: str, system: str | None = None, env: dict | None = None) -> list[Path]:
    system = system or platform.system()
    env = os.environ if env is None else env
    paths: list[Path] = []
    if env.get("PZ_UNITY"):
        paths.append(Path(env["PZ_UNITY"]))
    if system == "Windows":
        for base in (env.get("ProgramFiles", r"C:\Program Files"), r"C:\Program Files"):
            paths.append(Path(base) / "Unity" / "Hub" / "Editor" / version / "Editor" / "Unity.exe")
    elif system == "Darwin":
        paths.append(Path("/Applications/Unity/Hub/Editor") / version / "Unity.app/Contents/MacOS/Unity")
    else:
        paths.append(Path.home() / "Unity" / "Hub" / "Editor" / version / "Editor" / "Unity")
    seen, unique = set(), []
    for p in paths:
        if str(p) not in seen:
            seen.add(str(p))
            unique.append(p)
    return unique


def find_unity(version: str) -> Path | None:
    for path in candidate_paths(version):
        if path.is_file():
            return path
    return None


def android_module_paths(unity: Path) -> list[Path]:
    """Where Unity Hub puts Android Build Support for this editor (Windows/Linux: Editor/Data, macOS: next to Unity.app)."""
    paths = [unity.parent / "Data" / "PlaybackEngines" / "AndroidPlayer"]
    if len(unity.parents) >= 4 and unity.parents[2].name.endswith(".app"):
        paths.append(unity.parents[3] / "PlaybackEngines" / "AndroidPlayer")
    return paths


def has_android_module(unity: Path) -> bool:
    return any(p.is_dir() for p in android_module_paths(unity))


def results_path(command: str) -> Path:
    return LOGS / f"{TEST_PLATFORMS.get(command, 'editmode').lower()}-results.xml"


def command_line(unity: Path, command: str, log: Path, results: Path) -> list[str]:
    method, target = COMMANDS[command]
    args = [str(unity), "-batchmode", "-nographics", "-projectPath", str(PROJECT), "-logFile", str(log)]
    if target:
        args += ["-buildTarget", target]
    if method:
        args += ["-executeMethod", method]
    elif command in TEST_PLATFORMS:
        args += ["-runTests", "-testPlatform", TEST_PLATFORMS[command], "-testResults", str(results)]
    else:
        args += ["-quit"]  # open: import, resolve packages and compile, then exit
    return args


def pz_lines(log_text: str) -> list[str]:
    """The [PZ] lines EditorReport writes, without Unity's stack-trace noise."""
    out = []
    for line in log_text.splitlines():
        i = line.find("[PZ] ")
        if i >= 0:
            out.append(line[i + 5:].rstrip())
    return out


def compile_errors(log_text: str) -> list[str]:
    """Unique C# compiler error lines (file(line,col): error CSxxxx: message)."""
    seen, out = set(), []
    for line in log_text.splitlines():
        if "error CS" in line and line not in seen:
            seen.add(line)
            out.append(line.strip())
    return out


LICENCE_MARKERS = ("No valid Unity Editor license", "No ULF license found", "License is not active", "Licensing::Module")


def licence_problem(log_text: str) -> str | None:
    """The first licensing error line, if the editor refused to start for want of an activated licence."""
    for line in log_text.splitlines():
        if any(m in line for m in LICENCE_MARKERS) and ("error" in line.lower() or "no valid" in line.lower() or "not active" in line.lower()):
            return line.strip()
    return None


def summarise_nunit(xml_text: str) -> dict:
    root = ET.fromstring(xml_text)
    keys = ("total", "passed", "failed", "skipped", "inconclusive")
    summary = {k: int(root.get(k, 0)) for k in keys}
    summary["result"] = root.get("result", "?")
    summary["failures"] = [
        tc.get("fullname", tc.get("name", "?"))
        for tc in root.iter("test-case")
        if tc.get("result") == "Failed"
    ]
    return summary


def run_step(unity: Path, command: str) -> dict:
    """Run one command in its own editor session. Returns {status, exit, log, lines, ...}; status is PASS, FAIL or BLOCKED."""
    log = LOGS / f"unity-{command}.log"
    results = results_path(command)
    step = {"command": command, "log": str(log.relative_to(REPO))}
    if command in NEEDS_ANDROID:
        if not has_android_module(unity):
            step.update(status="BLOCKED", exit=None, lines=[
                "Android Build Support is not installed for this editor. Looked in: " + ", ".join(str(p) for p in android_module_paths(unity)),
                "Add it in Unity Hub (Installs > editor > Add modules: Android Build Support, OpenJDK, Android SDK & NDK Tools)."])
            return step
    if command in TEST_PLATFORMS and results.exists():
        results.unlink()

    code = subprocess.run(command_line(unity, command, log, results)).returncode
    text = log.read_text(encoding="utf-8", errors="replace") if log.exists() else ""
    step["exit"] = code
    step["lines"] = pz_lines(text)
    errors = compile_errors(text)
    if errors:
        step["compile_errors"] = errors[:200]
    licence = licence_problem(text)
    if code != 0 and licence:
        step.update(status="BLOCKED", note="Unity licence not activated (sign in with Unity Hub): " + licence)
        return step
    if "Scripts have compiler errors" in text and not errors:
        step["compile_errors"] = ["Unity reported 'Scripts have compiler errors'; see the log."]

    if command in TEST_PLATFORMS:
        if not results.exists():
            step.update(status="FAIL", note="no test results written; compile errors stop a test run")
            return step
        s = summarise_nunit(results.read_text(encoding="utf-8"))
        step["tests"] = s
        step["status"] = "PASS" if code == 0 and s["failed"] == 0 and s["total"] > 0 else "FAIL"
        return step

    if command == "open":
        lock = PROJECT / "Packages" / "packages-lock.json"
        step["packages_lock"] = lock.exists()
        step["status"] = "PASS" if code == 0 and not step.get("compile_errors") and lock.exists() else "FAIL"
        return step

    step["status"] = "PASS" if code == 0 and not step.get("compile_errors") else "FAIL"
    return step


def print_step(step: dict) -> None:
    print(f"== {step['command']}: {step['status']}" + (f" (exit {step['exit']})" if step.get("exit") is not None else ""))
    for line in step.get("lines", []):
        print("   " + line)
    for line in step.get("compile_errors", [])[:30]:
        print("   " + line)
    if "tests" in step:
        s = step["tests"]
        print(f"   {s['passed']}/{s['total']} passed, {s['failed']} failed, {s['skipped']} skipped ({s['result']})")
        for name in s["failures"]:
            print(f"   FAIL {name}")
    if step.get("note"):
        print("   " + step["note"])
    if step["command"] == "open":
        print("   packages-lock.json " + ("written" if step.get("packages_lock") else "MISSING"))


def first_run(unity: Path, version: str) -> int:
    """Every step in order. A failed 'open' (no compile) stops the run: nothing after it can be trusted."""
    steps = []
    for command in FIRST_RUN:
        if steps and steps[0]["status"] != "PASS":
            steps.append({"command": command, "status": "SKIPPED", "note": "open was " + steps[0]["status"]})
            continue
        print(f"Running Unity {version}: {command}")
        steps.append(run_step(unity, command))
        print_step(steps[-1])
    summary = {"editor": version, "unity": str(unity), "steps": steps}
    (LOGS / "first-run.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print("\nfirst-run summary (Game/Logs/PatientZero/first-run.json):")
    for s in steps:
        print(f"  {s['status']:8s} {s['command']}")
    return overall(steps)


def overall(steps: list[dict]) -> int:
    statuses = {s["status"] for s in steps}
    if "FAIL" in statuses:
        return 1
    if "SKIPPED" in statuses and "BLOCKED" not in statuses:
        return 1
    if "BLOCKED" in statuses:
        return BLOCKED
    return 0


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("command", choices=sorted(COMMANDS) + ["first-run"])
    parser.add_argument("--print-command", action="store_true", help="print the Unity command line and stop")
    args = parser.parse_args(argv)

    version = pinned_editor()
    unity = find_unity(version)
    LOGS.mkdir(parents=True, exist_ok=True)

    if unity is None:
        print(f"BLOCKED: Unity {version} not found. Looked in:")
        for p in candidate_paths(version):
            print(f"  {p}")
        print("Install it with Unity Hub (add Android Build Support) or set PZ_UNITY to its executable.")
        return BLOCKED

    if args.command == "first-run":
        if args.print_command:
            for command in FIRST_RUN:
                print(" ".join(f'"{c}"' if " " in c else c for c in
                               command_line(unity, command, LOGS / f"unity-{command}.log", results_path(command))))
            return 0
        return first_run(unity, version)

    if args.print_command:
        cmd = command_line(unity, args.command, LOGS / f"unity-{args.command}.log", results_path(args.command))
        print(" ".join(f'"{c}"' if " " in c else c for c in cmd))
        return 0

    print(f"Running Unity {version}: {args.command} (log: {LOGS / f'unity-{args.command}.log'})")
    step = run_step(unity, args.command)
    print_step(step)
    return {"PASS": 0, "FAIL": 1, "BLOCKED": BLOCKED}[step["status"]]


if __name__ == "__main__":
    sys.exit(main())
