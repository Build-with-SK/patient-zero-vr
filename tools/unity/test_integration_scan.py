"""Regression coverage for legal partial types and actual duplicate declarations."""
import json
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "review"))
import integration_scan


class PartialDeclarationTests(unittest.TestCase):
    def duplicates(self, first, second, same_assembly=True):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            game = root / "Game/Assets/PatientZero"
            a = game / "A"
            b = a if same_assembly else game / "B"
            for folder in {a, b}:
                folder.mkdir(parents=True)
                (folder / "Assembly.asmdef").write_text(json.dumps({"name": folder.name}), encoding="utf-8")
            (a / "First.cs").write_text("namespace Example { " + first + " }", encoding="utf-8")
            (b / "Second.cs").write_text("namespace Example { " + second + " }", encoding="utf-8")
            return [f for f in integration_scan.scan(root) if f.rule == "duplicate-type"]

    def test_partial_types_in_one_assembly_are_legal(self):
        for kind in ("class", "struct", "interface"):
            with self.subTest(kind=kind):
                self.assertEqual(self.duplicates(f"public partial {kind} Puzzle {{ }}",
                                                 f"public partial {kind} Puzzle {{ }}"), [])

    def test_ordinary_duplicate_types_still_fail(self):
        self.assertEqual(len(self.duplicates("public class Puzzle { }", "public class Puzzle { }")), 1)

    def test_every_declaration_must_be_partial(self):
        self.assertEqual(len(self.duplicates("public partial class Puzzle { }", "public class Puzzle { }")), 1)

    def test_partial_types_cannot_span_assemblies(self):
        self.assertEqual(len(self.duplicates("public partial class Puzzle { }",
                                             "public partial class Puzzle { }", same_assembly=False)), 1)

    def test_partial_declarations_must_have_the_same_kind(self):
        self.assertEqual(len(self.duplicates("public partial class Puzzle { }", "public partial struct Puzzle { }")), 1)


if __name__ == "__main__":
    unittest.main()
