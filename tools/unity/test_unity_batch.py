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

    def test_open_resolves_and_quits(self):
        cmd = ub.command_line(Path("Unity.exe"), "open", Path("l.log"), Path("r.xml"))
        self.assertIn("-quit", cmd)
        self.assertNotIn("-executeMethod", cmd)
        self.assertNotIn("-runTests", cmd)

    def test_playmode_command_and_results_file(self):
        cmd = ub.command_line(Path("Unity.exe"), "playmode-tests", Path("l.log"), ub.results_path("playmode-tests"))
        self.assertEqual(cmd[cmd.index("-testPlatform") + 1], "PlayMode")
        self.assertEqual(ub.results_path("playmode-tests").name, "playmode-results.xml")
        self.assertEqual(ub.results_path("editmode-tests").name, "editmode-results.xml")

    def test_first_run_order(self):
        self.assertEqual(set(ub.FIRST_RUN) - set(ub.COMMANDS), set())
        order = ub.FIRST_RUN
        self.assertEqual(order[0], "open")
        self.assertLess(order.index("import-samples"), order.index("compose"))  # samples compile before compose uses them
        self.assertLess(order.index("build-apk"), order.index("validate"))      # validate reads what configure wrote
        self.assertTrue({"configure", "build-apk", "validate"} <= ub.NEEDS_ANDROID)

    def test_android_module_paths(self):
        win = ub.android_module_paths(Path(r"C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe"))
        self.assertEqual(win[0], Path(r"C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Data\PlaybackEngines\AndroidPlayer"))
        mac = ub.android_module_paths(Path("/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app/Contents/MacOS/Unity"))
        self.assertIn(Path("/Applications/Unity/Hub/Editor/6000.3.27f1/PlaybackEngines/AndroidPlayer"), mac)

    def test_overall_status(self):
        def steps(*statuses):
            return [{"status": s} for s in statuses]
        self.assertEqual(ub.overall(steps("PASS", "PASS")), 0)
        self.assertEqual(ub.overall(steps("PASS", "BLOCKED")), ub.BLOCKED)
        self.assertEqual(ub.overall(steps("PASS", "FAIL", "BLOCKED")), 1)
        self.assertEqual(ub.overall(steps("BLOCKED", "SKIPPED")), ub.BLOCKED)  # licence missing: nothing after open ran
        self.assertEqual(ub.overall(steps("FAIL", "SKIPPED")), 1)

    def test_compile_errors_and_licence(self):
        log = ("Assets/A.cs(3,7): error CS0246: The type or namespace name 'Foo' could not be found\n"
               "Assets/A.cs(3,7): error CS0246: The type or namespace name 'Foo' could not be found\n"
               "warning CS0168: unused\n")
        self.assertEqual(len(ub.compile_errors(log)), 1)
        self.assertIsNone(ub.licence_problem(log))
        self.assertIsNotNone(ub.licence_problem("[Licensing::Module] Error: No valid Unity Editor license found.\n"))

    def test_pz_lines(self):
        log = "noise\n[PZ] PASS  saved Ward\nUnityEngine.Debug:Log\n2026 [PZ] FAIL  layout-missing: x\n"
        self.assertEqual(ub.pz_lines(log), ["PASS  saved Ward", "FAIL  layout-missing: x"])

    def test_nunit_summary(self):
        s = ub.summarise_nunit(NUNIT)
        self.assertEqual((s["total"], s["passed"], s["failed"]), (3, 2, 1))
        self.assertEqual(s["failures"], ["A.Bad"])


if __name__ == "__main__":
    unittest.main()
