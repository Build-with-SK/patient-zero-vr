#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
: "${UNITY_EDITOR:?Set UNITY_EDITOR to the licensed Unity 6000.0.66f2 executable}"
mkdir -p artifacts
for suite in EditMode PlayMode; do
    result="$(pwd)/artifacts/${suite}.xml"
    log="$(pwd)/artifacts/${suite}.log"
    rm -f "$result"
    "$UNITY_EDITOR" -batchmode -nographics -projectPath "$(pwd)/Tests/UnityProject" \
        -runTests -testPlatform "$suite" -testResults "$result" -logFile "$log"
    python3 - "$result" "$suite" <<'PY'
import sys
import xml.etree.ElementTree as ET
root = ET.parse(sys.argv[1]).getroot()
required = 29 if sys.argv[2] == 'EditMode' else 7
total = int(root.attrib.get('total', root.attrib.get('testcasecount', '0')))
passed = int(root.attrib.get('passed', '0'))
failed = int(root.attrib.get('failed', '0'))
skipped = int(root.attrib.get('skipped', '0'))
if total < required or passed < required or failed or skipped or root.attrib.get('result') != 'Passed':
    raise SystemExit(f"Unexpected {sys.argv[2]} outcome: {root.attrib}")
print(f"{sys.argv[2]}: {passed} passed; {failed} failed; {skipped} skipped")
PY
done
