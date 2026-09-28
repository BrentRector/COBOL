import unittest
from pricing import apply_discount, bulk_price, tax


class PricingTests(unittest.TestCase):
    def test_discount_reduces(self):
        self.assertEqual(apply_discount(200, 25), 150)

    def test_bulk_threshold_inclusive(self):
        self.assertEqual(bulk_price(10, 10), 95.0)

    def test_tax_is_only_the_tax(self):
        self.assertEqual(tax(100), 8.0)


if __name__ == "__main__":
    unittest.main()
