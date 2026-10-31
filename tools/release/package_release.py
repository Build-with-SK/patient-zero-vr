#!/usr/bin/env python3
"""Package the release artifacts that exist and write their checksums.

  python tools/release/package_release.py

Reads (Game/Builds, produced by tools/unity/unity_batch.py):
  Windows/PatientZero/            -> Release/PatientZero-<v>-windows-x64.zip (Burst debug info left out: not for shipping)
  Android/PatientZero-phone.apk   -> Release/PatientZero-<v>-android-phone.apk
  Android/PatientZero-dev.apk     -> Release/PatientZero-<v>-quest-dev.apk
  iOS/PatientZero-Xcode/          -> Release/PatientZero-<v>-ipad-xcode.zip (only when it was built)
Writes Release/SHA256SUMS.txt and Release/manifest.json (version, commit, sizes, checksums, what is missing).
Nothing is uploaded or published. Build folders are git-ignored.
"""
from __future__ import annotations

import hashlib
import json
import shutil
import subprocess
import sys
import zipfile
from datetime import datetime, timezone
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
BUILDS = REPO / "Game" / "Builds"
RELEASE = BUILDS / "Release"
VERSION = "1.0.0"
NOT_SHIPPED = ("_BurstDebugInformation_DoNotShip", "_BackUpThisFolder_ButDontShipItWithYourGame")


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            h.update(block)
    return h.hexdigest()


def zip_folder(folder: Path, target: Path, top: str) -> int:
    count = 0
    with zipfile.ZipFile(target, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as z:
        for path in sorted(folder.rglob("*")):
            rel = path.relative_to(folder)
            if any(part.endswith(NOT_SHIPPED) for part in rel.parts):
                continue
            if path.is_file():
                z.write(path, Path(top) / rel)
                count += 1
    return count


def commit() -> str:
    try:
        sha = subprocess.run(["git", "rev-parse", "HEAD"], cwd=REPO, capture_output=True, text=True, check=True).stdout.strip()
        dirty = subprocess.run(["git", "status", "--porcelain", "--", "Game/Assets", "Game/Packages", "Game/ProjectSettings"],
                               cwd=REPO, capture_output=True, text=True, check=True).stdout.strip()
        return sha + (" (+ uncommitted changes)" if dirty else "")
    except (OSError, subprocess.CalledProcessError):
        return "unknown"


def main() -> int:
    RELEASE.mkdir(parents=True, exist_ok=True)
    artifacts, missing = [], []

    windows = BUILDS / "Windows" / "PatientZero"
    if (windows / "PatientZero.exe").is_file():
        target = RELEASE / f"PatientZero-{VERSION}-windows-x64.zip"
        files = zip_folder(windows, target, f"PatientZero-{VERSION}-windows-x64")
        artifacts.append(("windows", target, f"{files} files; unzip, then run PatientZero.exe"))
    else:
        missing.append("windows: Game/Builds/Windows/PatientZero/PatientZero.exe")

    for source, name, kind in (("PatientZero-phone.apk", f"PatientZero-{VERSION}-android-phone.apk", "android-phone"),
                               ("PatientZero-dev.apk", f"PatientZero-{VERSION}-quest-dev.apk", "quest-dev")):
        src = BUILDS / "Android" / source
        if src.is_file():
            target = RELEASE / name
            shutil.copy2(src, target)
            artifacts.append((kind, target, "copy of Game/Builds/Android/" + source))
        else:
            missing.append(f"{kind}: Game/Builds/Android/{source}")

    xcode = BUILDS / "iOS" / "PatientZero-Xcode"
    if xcode.is_dir() and any(xcode.iterdir()):
        target = RELEASE / f"PatientZero-{VERSION}-ipad-xcode.zip"
        files = zip_folder(xcode, target, f"PatientZero-{VERSION}-ipad-xcode")
        artifacts.append(("ipad-xcode", target, f"{files} files; open Unity-iPhone.xcodeproj on a Mac, set the signing team, build"))
    else:
        missing.append("ipad: no Xcode project (iOS Build Support not installed, or build-ipad not run)")

    rows = []
    for kind, path, note in artifacts:
        rows.append({"kind": kind, "file": path.name, "path": str(path.relative_to(REPO)), "bytes": path.stat().st_size,
                     "sha256": sha256(path), "note": note})
    (RELEASE / "SHA256SUMS.txt").write_text("".join(f"{r['sha256']}  {r['file']}\n" for r in rows), encoding="utf-8")
    manifest = {"version": VERSION, "commit": commit(), "packaged": datetime.now(timezone.utc).isoformat(timespec="seconds"),
                "artifacts": rows, "missing": missing}
    (RELEASE / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    for r in rows:
        print(f"{r['kind']:14s} {r['bytes']:>12,d}  {r['sha256']}  {r['path']}")
    for m in missing:
        print("MISSING        " + m)
    print("wrote " + str((RELEASE / "manifest.json").relative_to(REPO)))
    return 0 if rows else 1


if __name__ == "__main__":
    sys.exit(main())
