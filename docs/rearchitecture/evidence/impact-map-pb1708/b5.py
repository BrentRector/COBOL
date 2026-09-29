"""The size of a CHEAPEST-FIRST first leg, from an uninstrumented battery trx (the PB1708 pivot, §3.13).

    python b5.py <conformance.trx> > b5.txt

INPUT NOT IN THE REPOSITORY: a `conformance.trx` from `scripts/battery.sh` (the committed b5.txt: battery #87,
2026-09-27). For a budget B (a share of the assembly's test-seconds), the first leg takes the cheapest tests while
their summed time stays within B. Reported per budget: how many tests (and what share of the population) that is,
how many collections they fall in, and the largest single collection's share of the leg — xunit runs one
collection serially, so that sum bounds the leg's wall clock from below however many cores there are.
"""
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict

NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"


def secs(d):
    h, m, s = d.split(":")
    return int(h) * 3600 + int(m) * 60 + float(s)


rows = []
for r in ET.parse(sys.argv[1]).getroot().iter(NS + "UnitTestResult"):
    name = r.get("testName")
    cls = name.split("(")[0].rsplit(".", 2)[-2]
    rows.append((secs(r.get("duration", "0:0:0")), cls, name))
rows.sort()
total = sum(d for d, _, _ in rows)
print(f"{len(rows)} tests, {total:.0f} test-seconds")
for budget in (0.01, 0.02, 0.05, 0.10):
    leg, acc = [], 0.0
    for d, c, n in rows:
        if acc + d > budget * total:
            break
        leg.append((d, c))
        acc += d
    per = defaultdict(float)
    for d, c in leg:
        per[c] += d
    big = max(per.items(), key=lambda kv: kv[1])
    print(f"budget {100 * budget:4.1f}% = {budget * total:6.0f} test-s: {len(leg):5d} tests ({100 * len(leg) / len(rows):4.1f}% "
          f"of the population) in {len(per):3d} collections; the largest collection's part {big[1]:5.1f} s ({big[0]})")
