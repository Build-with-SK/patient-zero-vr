"""Compile and run PATIENT ZERO pure-logic tests without the Unity Editor.

What it compiles (C# 9, the language version Unity 6 uses):
  - tools/logic_tests/stubs/*.cs and Runner.cs   (stand-ins for UnityEngine and NUnit)
  - Game/Assets/PatientZero/Core/**              (shared contracts and their tests)
  - Game/Assets/PatientZero/*/Logic/**           (each team's engine-free logic)
  - Game/Assets/PatientZero/*/Tests/EditMode/**  (each team's EditMode tests)

Passing here proves the logic compiles and behaves; it does NOT prove the Unity project
compiles. Only the Unity Editor can do that (see docs/TEST_REPORT.md).

Compiler: $PZ_CSC if set, else Roslyn csc.exe from Visual Studio / Build Tools, else a
.NET SDK via a generated project (the SDK route has not been exercised on this machine).

Usage:  python tools/logic_tests/run_logic_tests.py [name-filter] [--verbose]
"""

from __future__ import annotations

import glob
import os
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
HARNESS = ROOT / "tools" / "logic_tests"
GAME = ROOT / "Game" / "Assets" / "PatientZero"
OUT = HARNESS / "out"


def collect_sources() -> list[Path]:
    sources: set[Path] = set()
    sources.update(HARNESS.glob("stubs/*.cs"))
    sources.add(HARNESS / "Runner.cs")
    for path in (GAME / "Core").rglob("*.cs"):
        if "PlayMode" not in path.parts:
            sources.add(path)
    for area in GAME.iterdir() if GAME.exists() else []:
        if not area.is_dir() or area.name == "Core":
            continue
        sources.update((area / "Logic").rglob("*.cs"))
        sources.update((area / "Tests" / "EditMode").rglob("*.cs"))
    return sorted(sources)


def find_csc() -> Path | None:
    env = os.environ.get("PZ_CSC")
    if env and Path(env).is_file():
        return Path(env)
    patterns = [
        r"C:\Program Files*\Microsoft Visual Studio\*\*\MSBuild\Current\Bin\Roslyn\csc.exe",
    ]
    for pattern in patterns:
        hits = sorted(glob.glob(pattern), reverse=True)
        if hits:
            return Path(hits[0])
    return None


def has_dotnet_sdk() -> bool:
    exe = shutil.which("dotnet")
    if not exe:
        return False
    result = subprocess.run([exe, "--list-sdks"], capture_output=True, text=True)
    return bool(result.stdout.strip())


def build_with_csc(csc: Path, sources: list[Path]) -> Path:
    OUT.mkdir(parents=True, exist_ok=True)
    exe = OUT / "pz_logic_tests.exe"
    rsp = OUT / "build.rsp"
    lines = [
        "/nologo",
        "/langversion:9.0",
        "/target:exe",
        "/nullable:disable",
        "/warn:4",
        "/define:PZ_LOGIC_HARNESS",
        f'/out:"{exe}"',
    ] + [f'"{s}"' for s in sources]
    rsp.write_text("\n".join(lines), encoding="utf-8")
    result = subprocess.run([str(csc), f"@{rsp}"], capture_output=True, text=True)
    output = (result.stdout + result.stderr).strip()
    if output:
        print(output)
    if result.returncode != 0:
        raise SystemExit("Compile failed.")
    return exe


def build_with_dotnet(sources: list[Path]) -> list[str]:
    project_dir = OUT / "dotnet"
    project_dir.mkdir(parents=True, exist_ok=True)
    items = "\n".join(f'    <Compile Include="{s}" />' for s in sources)
    (project_dir / "PzLogicTests.csproj").write_text(
        f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <DefineConstants>PZ_LOGIC_HARNESS</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
{items}
  </ItemGroup>
</Project>
""",
        encoding="utf-8",
    )
    return ["dotnet", "run", "--project", str(project_dir), "--"]


def main() -> int:
    sources = collect_sources()
    print(f"Compiling {len(sources)} files ...", flush=True)
    csc = find_csc()
    if csc:
        command = [str(build_with_csc(csc, sources))]
    elif has_dotnet_sdk():
        command = build_with_dotnet(sources)
    else:
        print("No C# compiler found. Set PZ_CSC to a Roslyn csc.exe or install a .NET SDK.")
        return 2
    return subprocess.run(command + sys.argv[1:]).returncode


if __name__ == "__main__":
    sys.exit(main())
