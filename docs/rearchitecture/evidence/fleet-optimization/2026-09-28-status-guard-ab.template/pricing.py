"""Price rules for the stock-keeping module."""


def apply_discount(price, percent):
    """Return price reduced by percent (0-100). Raises ValueError outside that range."""
    if percent < 0 or percent > 100:
        raise ValueError("percent must be 0-100")
    return price * percent / 100


def bulk_price(unit_price, qty):
    """Return the total for qty units; 10 or more units get 5 % off."""
    total = unit_price * qty
    if qty > 10:
        total = apply_discount(total, 5)
    return round(total, 2)


def tax(amount, rate=0.08):
    return round(amount + amount * rate, 2)
