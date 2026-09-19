"""Static regression checks; run with python3 -m unittest discover -s Tests~.

Unity compilation remains the integration test. These checks catch known
sibling-namespace collisions quickly without installing the Unity editor.
"""
from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[1]


class NamespaceCollisionTests(unittest.TestCase):
    def assert_no_matches(self, pattern):
        for folder in ("Runtime", "Editor"):
            for path in (ROOT / folder).rglob("*.cs"):
                for number, line in enumerate(path.read_text().splitlines(), 1):
                    with self.subTest(path=str(path.relative_to(ROOT)), line=number):
                        self.assertIsNone(re.search(pattern, line), line)

    def test_unity_color_is_qualified(self):
        self.assert_no_matches(r"(?<![\w.])Color\b")

    def test_unity_time_members_are_qualified(self):
        # Timer.Time is an intentional instance property, not UnityEngine.Time.
        self.assert_no_matches(r"(?<![\w.])Time\.")

    def test_legacy_input_members_are_qualified(self):
        self.assert_no_matches(r"(?<![\w.])Input\.")


if __name__ == "__main__":
    unittest.main()
