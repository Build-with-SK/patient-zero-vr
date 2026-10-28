"""Tests for unity_batch.py (no Unity needed). Run: python -m unittest discover -s tools/unity -p "test_*.py" """
from __future__ import annotations

import sys
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import unity_batch as ub  # noqa: E402

NUNIT = """<?xml version="1.0" encoding="utf-8"?>
<test-run id="2" testcasecount="3" result="Failed(Child)" total="3" passed="2" failed="1" inconclusive="0" skipped="0">
  <test-suite type="Assembly" name="PatientZero.Integration.Tests.dll">
    <test-case name="Ok1" fullname="A.Ok1" result="Passed" />
    <test-case name="Ok2" fullname="A.Ok2" result="Passed" />
    <test-case name="Bad" fullname="A.Bad" result="Failed" />
  </test-suite>
</test-run>"""


class UnityBatchTests(unittest.TestCase):
    def test_pinned_editor_comes_from_the_policy(self):
        self.assertEqual(ub.pinned_editor(), "6000.3.27f1")

    def test_env_override_is_tried_first(self):
        paths = ub.candidate_paths("6000.3.27f1", "Windows", {"PZ_UNITY": r"D:\U\Unity.exe", "ProgramFiles": r"C:\Program Files"})
        self.assertEqual(paths[0], Path(r"D:\U\Unity.exe"))
        self.assertIn(Path(r"C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe"), paths)
        self.assertEqual(len(paths), len({str(p) for p in paths}))

    def test_mac_and_linux_paths(self):
        mac = ub.candidate_paths("6000.3.27f1", "Darwin", {})
        self.assertEqual(mac, [Path("/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app/Contents/MacOS/Unity")])
        linux = ub.candidate_paths("6000.3.27f1", "Linux", {})
        self.assertTrue(str(linux[0]).endswith(str(Path("Unity/Hub/Editor/6000.3.27f1/Editor/Unity"))))

    def test_execute_method_commands(self):
        cmd = ub.command_line(Path("Unity.exe"), "build-apk", Path("l.log"), Path("r.xml"))
        self.assertIn("-batchmode", cmd)
        self.assertEqual(cmd[cmd.index("-buildTarget") + 1], "Android")
        self.assertEqual(cmd[cmd.index("-executeMethod") + 1], "PatientZero.Integration.Editor.Quest.QuestBuild.BuildDevApkBatch")
        self.assertNotIn("-quit", cmd)  # the batch methods call EditorApplication.Exit with their own code

    def test_test_command_runs_editmode(self):
        cmd = ub.command_line(Path("Unity.exe"), "editmode-tests", Path("l.log"), Path("r.xml"))
        self.assertNotIn("-executeMethod", cmd)
        self.assertEqual(cmd[cmd.index("-testPlatform") + 1], "EditMode")

    def test_every_method_names_an_entry_point_that_exists_in_the_editor_code(self):
        editor = HERE.parents[1] / "Game/Assets/PatientZero/Integration/Editor"
        source = "\n".join(p.read_text(encoding="utf-8") for p in editor.rglob("*.cs"))
        for command, (method, _) in ub.COMMANDS.items():
            if method is None:
                continue
            type_name, member = method.rsplit(".", 1)
            namespace, cls = type_name.rsplit(".", 1)
            self.assertIn(f"namespace {namespace}", source, command)
            self.assertIn(f"class {cls}", source, command)
            self.assertRegex(source, rf"public static void {member}\(\)", command)

    def test_pz_lines(self):
        log = "noise\n[PZ] PASS  saved Ward\nUnityEngine.Debug:Log\n2026 [PZ] FAIL  layout-missing: x\n"
        self.assertEqual(ub.pz_lines(log), ["PASS  saved Ward", "FAIL  layout-missing: x"])

    def test_nunit_summary(self):
        s = ub.summarise_nunit(NUNIT)
        self.assertEqual((s["total"], s["passed"], s["failed"]), (3, 2, 1))
        self.assertEqual(s["failures"], ["A.Bad"])


if __name__ == "__main__":
    unittest.main()
