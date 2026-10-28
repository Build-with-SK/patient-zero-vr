#!/usr/bin/env python3
"""Check real asset reference integrity, WAV contents and deterministic generation.

These checks cannot replace a Unity import or SDK/headset validation.
"""
from pathlib import Path
import json
import re
import subprocess
import sys
import wave

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / 'Packages/com.patientzero.xr'


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def validate():
    assets = {}
    for meta in PACKAGE.rglob('*.meta'):
        match = re.search(r'^guid: ([a-f0-9]{32})$', meta.read_text(), re.M)
        require(match is not None, f'Invalid GUID in {meta}')
        key = match.group(1)
        require(key not in assets, f'Duplicate GUID in {meta}')
        asset = Path(str(meta)[:-5])
        require(asset.exists(), f'Orphan metadata {meta}')
        assets[key] = asset
    for path in PACKAGE.rglob('*'):
        if '~' in str(path.relative_to(PACKAGE)) or path.suffix == '.meta':
            continue
        require(Path(str(path) + '.meta').exists(), f'Missing metadata {path}')
        if path.suffix in ['.asmdef', '.json']:
            json.loads(path.read_text())
        if path.suffix not in ['.prefab', '.asset', '.mixer']:
            continue
        text = path.read_text()
        ids = re.findall(r'^--- !u!\d+ &(-?\d+)$', text, re.M)
        require(len(ids) == len(set(ids)), f'Duplicate serialized file IDs in {path}')
        for fields in re.findall(r'\{([^{}]*fileID:[^{}]*)\}', text):
            file_id = re.search(r'fileID: (-?\d+)', fields).group(1)
            guid = re.search(r'guid: ([a-f0-9]{32})', fields)
            if guid:
                require(guid.group(1) in assets, f'Unresolved asset GUID {fields} in {path}')
                target = assets[guid.group(1)]
                if target.suffix in ['.asset', '.mixer', '.prefab']:
                    require(re.search(r'^--- !u!\d+ &' + file_id + '$', target.read_text(), re.M), f'Unresolved external file ID {fields} in {path}')
                if target.suffix == '.cs':
                    require(file_id == '11500000', f'Invalid MonoScript file ID in {path}')
            else:
                require(file_id == '0' or file_id in ids, f'Unresolved local file ID {file_id} in {path}')
    wavs = list((PACKAGE / 'Audio/Diagnostic').glob('*.wav'))
    require(len(wavs) == 6, 'Expected all six diagnostic cues')
    for wav in wavs:
        with wave.open(str(wav)) as stream:
            require(stream.getnchannels() == 1 and stream.getnframes() > 0, f'Empty/stereo diagnostic clip {wav}')
    require(len(list((PACKAGE / 'Prefabs').glob('*.prefab'))) == 8, 'Expected eight template prefabs')
    before = {p.relative_to(PACKAGE): p.read_bytes() for p in PACKAGE.rglob('*') if p.is_file()}
    subprocess.run([sys.executable, str(ROOT / 'tools/generate_assets.py')], check=True)
    after = {p.relative_to(PACKAGE): p.read_bytes() for p in PACKAGE.rglob('*') if p.is_file()}
    require(before == after, 'Generated assets or metadata drifted; rerun generator and inspect the diff')
    print(f'PASS {len(assets)} unique asset GUIDs; all references resolve; 8 prefabs; 6 mono WAVs; generation repeatable')
    print('Unity import and SDK prefab generation remain separate required checks.')


if __name__ == '__main__':
    validate()
