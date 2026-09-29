"""Where an UNINSTRUMENTED whole-Conformance run spends its wall clock, from a battery trx (the PB1708 pivot, M6/M7).

    python b1.py <conformance.trx> > b1.txt

INPUT NOT IN THE REPOSITORY: a `conformance.trx` written by `scripts/battery.sh` (PHASE 1, `--logger trx`). The
committed `b1.txt` was measured on battery #87 (2026-09-27, Normal priority, Conformance ∥ Unit ∥ Characterization
on the shared 32-logical-core host). Any later battery's trx re-measures it.

The xunit adapter stamps startTime ≈ endTime, so a test's interval is [end - duration, end]. Reports:
  1. the population, the wall, the test-seconds and the average concurrency (test-seconds ÷ wall);
  2. test-seconds by test METHOD (theory rows summed) and by COLLECTION (class = collection, xunit v2 default);
  3. the concurrency profile: how long the run spent with fewer than k tests running (the TAIL);
  4. the long poles: single tests and whole collections against the wall.
"""
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from datetime import datetime

NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"


def secs(d):
    h, m, s = d.split(":")
    return int(h) * 3600 + int(m) * 60 + float(s)


def main(path):
    rows = []
    for r in ET.parse(path).getroot().iter(NS + "UnitTestResult"):
        name = r.get("testName")
        dur = secs(r.get("duration", "0:0:0"))
        end = datetime.fromisoformat(r.get("endTime")).timestamp()
        head = name.split("(")[0]
        cls, meth = head.rsplit(".", 2)[-2:]
        rows.append((name, cls, meth, dur, end - dur, end))
    t0 = min(r[4] for r in rows)
    t1 = max(r[5] for r in rows)
    wall = t1 - t0
    total = sum(r[3] for r in rows)
    print(f"{len(rows)} tests, wall {wall:.0f} s, {total:.0f} test-seconds, average concurrency {total / wall:.1f}x")

    by_meth = defaultdict(lambda: [0, 0.0])
    by_coll = defaultdict(lambda: [0, 0.0, 1e18, 0.0])
    for name, cls, meth, dur, s, e in rows:
        k = f"{cls.split('_P')[0]}.{meth}"
        by_meth[k][0] += 1
        by_meth[k][1] += dur
        c = by_coll[cls]
        c[0] += 1
        c[1] += dur
        c[2] = min(c[2], s)
        c[3] = max(c[3], e)
    print("\n-- test-seconds by METHOD (theory rows summed), top 12")
    for k, (n, t) in sorted(by_meth.items(), key=lambda kv: -kv[1][1])[:12]:
        print(f"{t:8.0f} s {100 * t / total:5.1f}%  {n:5d} rows {t / n:7.2f} s/row  {k}")
    print("\n-- COLLECTIONS (class = collection): test-seconds, and when each started/ended relative to the run")
    colls = sorted(by_coll.items(), key=lambda kv: -kv[1][1])
    for cls, (n, t, s, e) in colls[:24]:
        print(f"{t:8.0f} s  {n:5d} tests  start +{s - t0:5.0f} s  end +{e - t0:5.0f} s  {cls}")
    print(f"   ... {len(colls)} collections in all")

    print("\n-- the concurrency profile (tests running, sampled every second)")
    n = int(wall) + 1
    running = [0] * n
    for *_, s, e in rows:
        for i in range(int(s - t0), min(n, int(e - t0) + 1)):
            running[i] += 1
    for k in (32, 24, 16, 8, 4, 2):
        below = sum(1 for x in running if x < k)
        print(f"   fewer than {k:2d} tests running: {below:5d} s of {n} s ({100 * below / n:4.1f}%)")
    last16 = max((i for i, x in enumerate(running) if x >= 16), default=0)
    print(f"   the last second with >= 16 tests running: +{last16} s; the tail after it: {n - last16} s")
    for q in range(0, n, max(1, n // 20)):
        print(f"     +{q:5d} s  {running[q]:3d} running")

    print("\n-- long poles: the slowest single tests")
    for name, cls, meth, dur, s, e in sorted(rows, key=lambda r: -r[3])[:10]:
        print(f"{dur:8.1f} s  start +{s - t0:5.0f} s  {name[:150]}")
    lb = max(total / 32, max(t for _, (_, t, _, _) in colls))
    print(f"\nlower bound on the wall for THIS work at 32 threads (max(test-s / 32, the longest collection)): {lb:.0f} s")


if __name__ == "__main__":
    for stream in (sys.stdout,):
        stream.reconfigure(encoding="utf-8", errors="replace")
    main(sys.argv[1])
