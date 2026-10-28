#!/usr/bin/env python3
"""Run the PatientZero editor tooling in Unity batchmode with the pinned editor, and read the results back.

  python tools/unity/unity_batch.py compose        # build Bootstrap.unity + Ward.unity, validate layout and services
  python tools/unity/unity_batch.py validate       # editor, packages, URP, Android, XR loader, OpenXR features
  python tools/unity/unity_batch.py configure      # apply the Quest configuration (writes ProjectSettings and Assets/XR)
  python tools/unity/unity_batch.py desktop-sim    # import the XRI simulator samples and switch to desktop simulation
  python tools/unity/unity_batch.py build-apk      # configure + compose + validate + Builds/Android/PatientZero-dev.apk
  python tools/unity/unity_batch.py editmode-tests # every EditMode test assembly, NUnit XML in Game/Logs/PatientZero

The editor is found from $PZ_UNITY, else the Unity Hub install folder for the pinned version.
Exit codes: 0 pass, 1 Unity ran and reported failure, 2 BLOCKED (no pinned editor on this machine).
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

COMMANDS = {
    "compose": ("PatientZero.Integration.Editor.SceneComposer.BuildAllBatch", None),
    "validate": ("PatientZero.Integration.Editor.Quest.QuestBuild.ValidateBatch", None),
    "configure": ("PatientZero.Integration.Editor.Quest.QuestBuild.ConfigureBatch", "Android"),
    "desktop-sim": ("PatientZero.Integration.Editor.Quest.DesktopSimulation.EnableBatch", None),
    "build-apk": ("PatientZero.Integration.Editor.Quest.QuestBuild.BuildDevApkBatch", "Android"),
    "editmode-tests": (None, None),
}


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


def command_line(unity: Path, command: str, log: Path, results: Path) -> list[str]:
    method, target = COMMANDS[command]
    args = [str(unity), "-batchmode", "-nographics", "-projectPath", str(PROJECT), "-logFile", str(log)]
    if target:
        args += ["-buildTarget", target]
    if method:
        args += ["-executeMethod", method]
    else:
        args += ["-runTests", "-testPlatform", "EditMode", "-testResults", str(results)]
    return args


def pz_lines(log_text: str) -> list[str]:
    """The [PZ] lines EditorReport writes, without Unity's stack-trace noise."""
    out = []
    for line in log_text.splitlines():
        i = line.find("[PZ] ")
        if i >= 0:
            out.append(line[i + 5:].rstrip())
    return out


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


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("command", choices=sorted(COMMANDS))
    parser.add_argument("--print-command", action="store_true", help="print the Unity command line and stop")
    args = parser.parse_args(argv)

    version = pinned_editor()
    unity = find_unity(version)
    LOGS.mkdir(parents=True, exist_ok=True)
    log = LOGS / f"unity-{args.command}.log"
    results = LOGS / "editmode-results.xml"

    if unity is None:
        print(f"BLOCKED: Unity {version} not found. Looked in:")
        for p in candidate_paths(version):
            print(f"  {p}")
        print("Install it with Unity Hub (add Android Build Support) or set PZ_UNITY to its executable.")
        return BLOCKED

    cmd = command_line(unity, args.command, log, results)
    if args.print_command:
        print(" ".join(f'"{c}"' if " " in c else c for c in cmd))
        return 0

    print(f"Running Unity {version}: {args.command} (log: {log})")
    code = subprocess.run(cmd).returncode
    text = log.read_text(encoding="utf-8", errors="replace") if log.exists() else ""
    for line in pz_lines(text):
        print(line)

    if args.command == "editmode-tests":
        if not results.exists():
            print("FAIL: no test results written; see the log (compile errors stop the run).")
            return 1
        s = summarise_nunit(results.read_text(encoding="utf-8"))
        print(f"EditMode: {s['passed']}/{s['total']} passed, {s['failed']} failed, {s['skipped']} skipped ({s['result']})")
        for name in s["failures"]:
            print(f"  FAIL {name}")
        return 0 if code == 0 and s["failed"] == 0 else 1

    if "error CS" in text or "Scripts have compiler errors" in text:
        print("FAIL: Unity reported compile errors; see the log.")
    print(f"Unity exit code {code}")
    return 0 if code == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
