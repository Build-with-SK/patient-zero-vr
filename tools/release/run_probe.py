#!/usr/bin/env python3
"""Release QA on the built Windows game, not the Editor.

  python tools/release/run_probe.py                 # 3 full rounds, windowed 1280x720
  python tools/release/run_probe.py --rounds 5 --runs 2

Starts Game/Builds/Windows/PatientZero/PatientZero.exe with `-pz-autoplay N` (ReleaseProbe: New game from the main menu,
every act's objectives, Play again between rounds, then quit), waits for it to exit, and reads its log back:
the "[PZ-PROBE]" lines, the exit code (0 = every round reached the ending with no error or exception logged) and any
error lines. Logs go to Game/Logs/PatientZero/release-probe-<n>.log; a JSON summary to release-probe.json.
Exit code: 0 when every run passed, 1 otherwise, 2 when the exe does not exist.
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import time
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
EXE = REPO / "Game" / "Builds" / "Windows" / "PatientZero" / "PatientZero.exe"
LOGS = REPO / "Game" / "Logs" / "PatientZero"
ERROR_LINE = re.compile(r"(Exception|NullReference|MissingReference|MissingComponent|\bError\b|Assertion failed)", re.I)
IGNORED = ("[PZ-PROBE]",)


def probe_lines(text: str) -> list[str]:
    return [line.strip() for line in text.splitlines() if "[PZ-PROBE]" in line]


def error_lines(text: str) -> list[str]:
    out = []
    for line in text.splitlines():
        if any(i in line for i in IGNORED):
            continue
        if ERROR_LINE.search(line):
            out.append(line.strip())
    return out


def run_once(exe: Path, rounds: int, index: int, timeout: float, width: int, height: int, shots: Path | None = None) -> dict:
    log = LOGS / f"release-probe-{index}.log"
    if log.exists():
        log.unlink()
    args = [str(exe), "-pz-autoplay", str(rounds), "-screen-fullscreen", "0", "-screen-width", str(width),
            "-screen-height", str(height), "-logFile", str(log)]
    if shots:
        args += ["-pz-shots", str(shots)]
    started = time.time()
    try:
        code = subprocess.run(args, timeout=timeout).returncode
        timed_out = False
    except subprocess.TimeoutExpired:
        code, timed_out = None, True
    seconds = round(time.time() - started, 1)
    text = log.read_text(encoding="utf-8", errors="replace") if log.exists() else ""
    lines = probe_lines(text)
    passed = code == 0 and any("RESULT PASS" in l for l in lines)
    return {"run": index, "rounds": rounds, "exit": code, "timed_out": timed_out, "seconds": seconds,
            "log": str(log.relative_to(REPO)), "probe": lines, "errors": error_lines(text)[:20],
            "status": "PASS" if passed else "FAIL"}


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--rounds", type=int, default=3)
    parser.add_argument("--runs", type=int, default=1)
    parser.add_argument("--timeout", type=float, default=0, help="seconds per run (default: 360 per round + 60)")
    parser.add_argument("--width", type=int, default=1280)
    parser.add_argument("--height", type=int, default=720)
    parser.add_argument("--exe", type=Path, default=EXE)
    parser.add_argument("--shots", action="store_true", help="save screenshots of round 1 to Game/Logs/PatientZero/shots")
    args = parser.parse_args(argv)
    if not args.exe.is_file():
        print(f"BLOCKED: {args.exe} does not exist. Build it: python tools/unity/unity_batch.py build-windows")
        return 2
    LOGS.mkdir(parents=True, exist_ok=True)
    timeout = args.timeout or 360 * args.rounds + 60
    results = []
    for i in range(1, args.runs + 1):
        print(f"run {i}/{args.runs}: {args.rounds} round(s), timeout {timeout:.0f} s")
        r = run_once(args.exe, args.rounds, i, timeout, args.width, args.height, LOGS / "shots" if args.shots else None)
        results.append(r)
        print(f"  {r['status']}  exit {r['exit']}  {r['seconds']} s  log {r['log']}")
        for line in r["probe"]:
            print("    " + line)
        for line in r["errors"][:5]:
            print("    ERROR LINE: " + line)
    (LOGS / "release-probe.json").write_text(json.dumps(results, indent=2), encoding="utf-8")
    ok = all(r["status"] == "PASS" for r in results)
    print(("PASS" if ok else "FAIL") + f": {sum(r['status'] == 'PASS' for r in results)}/{len(results)} run(s)")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
