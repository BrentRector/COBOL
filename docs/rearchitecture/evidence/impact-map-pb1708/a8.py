"""Test-seconds by test METHOD (theory rows summed) — the heaviest methods of the Conformance assembly."""
from collections import defaultdict
from common import *

m, ix = load()
by = defaultdict(lambda: [0, 0.0])
for i in ix.by_assembly["Conformance"]:
    t = ix.tests[i]
    meth = t[1].split(".")[-1]
    cls = t[3].split(".")[-1].split("_P")[0]
    by[f"{cls}.{meth}"][0] += 1
    by[f"{cls}.{meth}"][1] += t[5]
tot = sum(v[1] for v in by.values())
for k, (n, s) in sorted(by.items(), key=lambda kv: -kv[1][1])[:12]:
    print(f"{s:8.0f} s {100 * s / tot:5.1f}%  {n:5d} rows  {s / n:6.2f} s/row  {k}")
