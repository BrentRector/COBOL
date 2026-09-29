"""Static constructors in src/ owning >= 40 source lines (static TABLES, by the owned-line measure), with fan-out."""
from common import *

m, ix = load()
rows = []
for i, (name, segs) in enumerate(ix.entries):
    if "::.cctor" not in name:
        continue
    lines = sum(hi - lo + 1 for _, lo, hi in segs if lo > 0)
    f = sorted({s[0] for s in segs})[0] if segs else "?"
    if lines >= 40 and f.startswith("src/"):
        rows.append((lines, count(ix, ix.implied_by.get(i, 0) | (1 << i)), name.split(" ")[1].split("::")[0], f))
for r in sorted(rows, reverse=True):
    print(f"{r[0]:5d} lines  {r[1]:5d} tests  {r[2]}  ({r[3]})")
