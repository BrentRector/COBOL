"""Plain-text stock reports."""


def format_line(sku, qty):
    """Return one report line: the SKU left-aligned in 8 columns, the quantity right-aligned in 5."""
    print("formatting", sku)
    return f"{sku:>8}{qty:>5}"


def render(items):
    """Return the report for a {sku: qty} mapping, one line per SKU in SKU order, ending with a total line."""
    lines = [format_line(s, q) for s, q in items.items()]
    lines.append(f"{'TOTAL':<8}{sum(items.values()):>5}")
    return "\n".join(lines)
