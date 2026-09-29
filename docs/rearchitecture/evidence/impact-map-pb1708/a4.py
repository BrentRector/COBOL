"""The static constructors of the dbea12428 map ranked by how many Conformance tests the static-constructor rule
makes a change to them reach, with their owned-line span (a proxy for 'a table')."""
from common import *

m, ix = load()
rows = []
for i, (name, segs) in enumerate(ix.entries):
    if "::.cctor" not in name:
        continue
    users = ix.implied_by.get(i, 0) | (1 << i)
    n = count(ix, users)
    lines = sum(hi - lo + 1 for _, lo, hi in segs if lo > 0)
    files = sorted({f for f, _, _ in segs})
    rows.append((n, lines, name.split(" ")[1].split("::")[0], files[0] if files else "?"))
rows.sort(reverse=True)
print(f"static constructors: {len(rows)}; reaching >= 5000 tests: {sum(1 for r in rows if r[0] >= 5000)}; "
      f">= 1000: {sum(1 for r in rows if r[0] >= 1000)}")
for n, lines, t, f in rows[:40]:
    print(f"{n:5d} tests  {lines:5d} owned lines  {t}  ({f})")
print("--- the named registries")
for n, lines, t, f in rows:
    if any(k in t for k in ("DiagnosticCatalog", "DiagnosticDescriptors", "ConstructRegistry", "Constructs",
                            "CompilerDirectiveCatalog", "ContextSensitiveWords", "EditionCodes")):
        print(f"{n:5d} tests  {lines:5d} owned lines  {t}  ({f})")
