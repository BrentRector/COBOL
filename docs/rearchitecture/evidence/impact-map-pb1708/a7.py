"""Where the Conformance assembly's recorded test time goes (dbea12428 recording, instrumented, BelowNormal):
the classes carrying the most test-seconds, and the test-count / time shares of the heaviest."""
from collections import defaultdict
from common import *

m, ix = load()
by = defaultdict(lambda: [0, 0.0])
for i in ix.conf:
    t = ix.tests[i]
    by[t[3]][0] += 1
    by[t[3]][1] += t[5]
tot_n = len(ix.conf)
tot_s = sum(v[1] for v in by.values())
rows = sorted(by.items(), key=lambda kv: -kv[1][1])
print(f"{tot_n} tests, {tot_s:.0f} test-seconds, {len(by)} classes")
acc = 0.0
for k, (n, s) in rows[:15]:
    acc += s
    print(f"{s:8.0f} s {100 * s / tot_s:5.1f}%  (cum {100 * acc / tot_s:5.1f}%)  {n:5d} tests  {k.split('.')[-1]}")
secs = sorted((ix.tests[i][5] for i in ix.conf), reverse=True)
for q in (0.01, 0.1, 0.5):
    k = int(tot_n * q)
    print(f"slowest {q:.0%} of tests ({k}) carry {100 * sum(secs[:k]) / tot_s:.1f}% of the time")
