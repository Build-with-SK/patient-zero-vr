"""Tests for validate_packages.py. Run: python -m unittest discover -s tools/unity -p "test_*.py" """
from __future__ import annotations

import json
import shutil
import sys
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
sys.path.insert(0, str(HERE))

import validate_packages as vp  # noqa: E402


def errors(findings):
    return [f for f in findings if f.severity == "ERROR"]


class Fixture:
    """A copy of the four files the validator reads, in a temp dir, editable per test."""

    def __init__(self):
        self.dir = Path(tempfile.mkdtemp(prefix="pz_pkg_"))
        for rel in (vp.POLICY, vp.MANIFEST, vp.PROJECT_VERSION, vp.QUEST_CONFIG):
            (self.dir / rel).parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(REPO / rel, self.dir / rel)

    def manifest(self) -> dict:
        return json.loads((self.dir / vp.MANIFEST).read_text(encoding="utf-8"))

    def write_manifest(self, data: dict) -> None:
        (self.dir / vp.MANIFEST).write_text(json.dumps(data, indent=2), encoding="utf-8")

    def write_lock(self, deps: dict) -> None:
        (self.dir / vp.LOCK).write_text(json.dumps({"dependencies": deps}), encoding="utf-8")

    def edit(self, rel: Path, old: str, new: str) -> None:
        path = self.dir / rel
        text = path.read_text(encoding="utf-8")
        assert old in text, f"{old!r} not in {rel}"
        path.write_text(text.replace(old, new), encoding="utf-8")

    def cleanup(self) -> None:
        shutil.rmtree(self.dir, ignore_errors=True)


class ValidatePackagesTests(unittest.TestCase):
    def setUp(self):
        self.fx = Fixture()

    def tearDown(self):
        self.fx.cleanup()

    def test_repository_files_pass(self):
        self.assertEqual(errors(vp.check(REPO)), [])

    def test_fixture_copy_passes(self):
        self.assertEqual(errors(vp.check(self.fx.dir)), [])

    def test_missing_required_package(self):
        m = self.fx.manifest()
        del m["dependencies"]["com.unity.xr.hands"]
        self.fx.write_manifest(m)
        self.assertEqual([f.rule for f in errors(vp.check(self.fx.dir))], ["package-missing"])

    def test_wrong_version(self):
        m = self.fx.manifest()
        m["dependencies"]["com.unity.xr.openxr"] = "1.18.0"
        self.fx.write_manifest(m)
        self.assertEqual([f.rule for f in errors(vp.check(self.fx.dir))], ["package-version"])

    def test_forbidden_direct_dependency(self):
        m = self.fx.manifest()
        m["dependencies"]["com.unity.xr.oculus"] = "4.5.5"
        m["dependencies"]["com.meta.xr.sdk.interaction.ovr"] = "85.0.0"
        self.fx.write_manifest(m)
        found = errors(vp.check(self.fx.dir))
        self.assertEqual([f.rule for f in found], ["package-forbidden", "package-forbidden"])

    def test_held_back_audio_sdk_is_not_forbidden_by_prefix(self):
        self.assertFalse(vp.is_forbidden("com.meta.xr.sdk.audio", ["com.meta.xr.sdk.core"]))
        self.assertTrue(vp.is_forbidden("com.meta.xr.sdk.core", ["com.meta.xr.sdk.core"]))
        self.assertTrue(vp.is_forbidden("com.meta.xr.sdk.core.extra", ["com.meta.xr.sdk.core"]))
        self.assertFalse(vp.is_forbidden("com.meta.xr.sdk.coreish", ["com.meta.xr.sdk.core"]))

    def test_forbidden_transitive_dependency_in_lock(self):
        self.fx.write_lock({"com.unity.xr.oculus": {"version": "4.5.5", "depth": 1}})
        found = errors(vp.check(self.fx.dir))
        self.assertEqual([f.rule for f in found], ["package-forbidden"])
        self.assertIn("depth 1", found[0].message)

    def test_lock_version_drift(self):
        self.fx.write_lock({"com.unity.xr.management": {"version": "4.8.0", "depth": 0}})
        self.assertEqual([f.rule for f in errors(vp.check(self.fx.dir))], ["lock-version"])

    def test_git_and_file_sources_rejected(self):
        m = self.fx.manifest()
        m["dependencies"]["com.example.tool"] = "https://github.com/example/tool.git"
        m["dependencies"]["com.example.local"] = "file:../local"
        self.fx.write_manifest(m)
        self.assertEqual([f.rule for f in errors(vp.check(self.fx.dir))], ["package-source", "package-source"])

    def test_scoped_registry_rejected(self):
        m = self.fx.manifest()
        m["scopedRegistries"] = [{"name": "Meta", "url": "https://npm.developer.oculus.com", "scopes": ["com.meta.xr"]}]
        self.fx.write_manifest(m)
        self.assertEqual([f.rule for f in errors(vp.check(self.fx.dir))], ["scoped-registry"])

    def test_editor_version_mismatch(self):
        self.fx.edit(vp.PROJECT_VERSION, "6000.3.27f1 (", "6000.3.26f1 (")
        self.fx.edit(vp.PROJECT_VERSION, "m_EditorVersion: 6000.3.27f1", "m_EditorVersion: 6000.3.26f1")
        self.assertEqual([f.rule for f in errors(vp.check(self.fx.dir))], ["editor-version"])

    def test_editor_revision_mismatch(self):
        self.fx.edit(vp.PROJECT_VERSION, "672d562a42a7", "000000000000")
        self.assertEqual([f.rule for f in errors(vp.check(self.fx.dir))], ["editor-revision"])

    def test_quest_config_drift(self):
        self.fx.edit(vp.QUEST_CONFIG, '"com.meta.xr.sdk.platform",', "")
        self.assertEqual([f.rule for f in errors(vp.check(self.fx.dir))], ["config-drift"])

    def test_missing_file(self):
        (self.fx.dir / vp.MANIFEST).unlink()
        self.assertEqual([f.rule for f in errors(vp.check(self.fx.dir))], ["missing-file"])

    def test_unity_supported(self):
        self.assertTrue(vp.unity_supported("2022.3", "6000.3.27f1"))
        self.assertTrue(vp.unity_supported("6000.3", "6000.3.27f1"))
        self.assertFalse(vp.unity_supported("6000.4", "6000.3.27f1"))
        self.assertTrue(vp.unity_supported(None, "6000.3.27f1"))

    def test_online_with_fake_registry(self):
        published = {"versions": {"1.19.0": {"unity": "6000.0"}}, "dist-tags": {"latest": "1.20.0"}}

        def fetch(url):
            if url.endswith("/com.unity.xr.openxr"):
                return published
            if url.endswith("/com.unity.xr.hands"):
                return {"versions": {"1.10.0": {"unity": "6000.4"}}}
            return {"versions": {}}

        found = vp.check_online(self.fx.dir, fetch)
        by_rule = {}
        for f in found:
            by_rule.setdefault(f.rule, []).append(f)
        self.assertTrue(any("com.unity.xr.openxr 1.19.0 published" in f.message and "latest 1.20.0" in f.message for f in by_rule["online"]))
        self.assertEqual([f.message.split()[0] for f in by_rule["online-unity"]], ["com.unity.xr.hands"])
        self.assertGreaterEqual(len(by_rule["online-missing"]), 1)
        self.assertTrue(all("editorLocked" not in f.message for f in found))


if __name__ == "__main__":
    unittest.main()
