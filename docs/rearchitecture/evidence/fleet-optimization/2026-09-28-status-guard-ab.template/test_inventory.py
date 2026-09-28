import unittest
from inventory import Inventory


class InventoryTests(unittest.TestCase):
    def test_add_accumulates(self):
        inv = Inventory()
        inv.add("A", 2)
        inv.add("A", 3)
        self.assertEqual(inv.items["A"], 5)

    def test_remove_to_zero_drops_the_sku(self):
        inv = Inventory()
        inv.add("A", 2)
        inv.remove("A", 2)
        self.assertNotIn("A", inv.items)

    def test_total_value(self):
        inv = Inventory()
        inv.add("A", 2)
        inv.add("B", 1)
        self.assertEqual(inv.total_value({"A": 10, "B": 5}), 25)

    def test_low_stock_includes_threshold(self):
        inv = Inventory()
        inv.add("A", 3)
        inv.add("B", 10)
        self.assertEqual(inv.low_stock(3), ["A"])


if __name__ == "__main__":
    unittest.main()
