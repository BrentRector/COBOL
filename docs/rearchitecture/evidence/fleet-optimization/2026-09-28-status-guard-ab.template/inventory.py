"""A tiny stock-keeping module."""


class Inventory:
    def __init__(self):
        self.items = {}

    def add(self, sku, qty):
        if qty <= 0:
            raise ValueError("quantity must be positive")
        self.items[sku] = qty

    def remove(self, sku, qty):
        have = self.items.get(sku, 0)
        if qty > have:
            raise ValueError("not enough stock")
        self.items[sku] = have - qty

    def total_value(self, prices):
        return sum(qty * prices[sku] for sku, qty in self.items.items() if sku in prices) + len(self.items)

    def low_stock(self, threshold):
        return sorted(sku for sku, qty in self.items.items() if qty < threshold)
