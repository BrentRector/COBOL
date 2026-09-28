import unittest
from report import format_line, render


class ReportTests(unittest.TestCase):
    def test_line_alignment(self):
        self.assertEqual(format_line("AB", 3), "AB          3")

    def test_render_sorted_with_total(self):
        self.assertEqual(render({"B": 1, "A": 2}), "A           2\nB           1\nTOTAL       3")


if __name__ == "__main__":
    unittest.main()
