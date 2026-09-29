"""The PB1708 pivot's ORDERING, replayed on the one dropped branch whose reds are named: train 68b X.

    python b4.py > b4.txt           (needs the 4b0f3e22a map; see common.py for how to record it)

X (`5171c6872`, base `4b0f3e22a`) went red at the lander's whole-assembly gate on two tests its name-guessed filter
never ran: DiagnosticPositionTests.Copy_MainLinesStayMain_CopiedLinesNameTheCopybook and
ParseErrorInsideCopybook_NamesTheCopybookLine. Ordered by the §3.13 rank (tier 1: tests executing a directly
changed method body, by specificity; tier 2: tests reached only through a widening — a static constructor, a
declaration change, a file-level fallback; tier 3: the rest), where would each red have run, and after how many
test-seconds? Specificity of a changed entry e is idf(e) = ln(N / df(e)), df(e) = the Conformance tests that
executed it; a test's score is the sum over the changed entries it executed; ties break by shorter recorded time.
Durations are the map recording's per-test times (instrumented, BelowNormal), so compare them as SHARES.
"""
import math
from common import *

m, ix = load("4b0f3e22ae272ae6f5163b42b45b0bbd3ad1d707")
M = m["commit"]
RED = ["DiagnosticPositionTests.Copy_MainLinesStayMain_CopiedLinesNameTheCopybook",
       "DiagnosticPositionTests.ParseErrorInsideCopybook_NamesTheCopybookLine"]
N = len(ix.conf)
dur = {i: ix.tests[i][5] for i in ix.conf}
total = sum(dur.values())

base = git("rev-parse", "4b0f3e22a").strip()
head = git("rev-parse", "5171c6872").strip()
rows = per_file(ix, M, base, head)
direct = 0
widened = 0
for s, p, mask, whole, notes, extra in rows:
    n0 = notes[0] if notes else ""
    if "changed method" in n0 and "file level" not in n0:
        direct |= mask
    else:
        widened |= mask
print(f"68b X: {len(rows)} changed files; map {M[:12]}; {N} Conformance tests, {total:.0f} recorded test-seconds")

entries = [e for e in range(len(ix.entries)) if direct >> e & 1]
df = {e: sum(1 for i in ix.conf if ix.conf_bits[i] >> e & 1) for e in entries}
idf = {e: math.log(N / df[e]) for e in entries if df[e]}
score = {i: sum(w for e, w in idf.items() if ix.conf_bits[i] >> e & 1) for i in ix.conf}
tier = {}
for i in ix.conf:
    if score[i] > 0:
        tier[i] = 1
    elif ix.conf_bits[i] & widened:
        tier[i] = 2
    else:
        tier[i] = 3
order = sorted(ix.conf, key=lambda i: (tier[i], -score[i], dur[i]))
for t in (1, 2, 3):
    ids = [i for i in ix.conf if tier[i] == t]
    print(f"  tier {t}: {len(ids):5d} tests, {sum(dur[i] for i in ids):7.0f} test-s "
          f"({100 * sum(dur[i] for i in ids) / total:4.1f}%)")
print(f"  changed entries executed by some test: {len(idf)}; median df {sorted(df.values())[len(df) // 2] if df else 0}")

RARE = 0.05 * N   # an entry fewer than 5 % of the tests execute is SPECIFIC to them
rare = {i: sum(w for e, w in idf.items() if df[e] <= RARE and ix.conf_bits[i] >> e & 1) for i in ix.conf}
ORDERS = {
    "A  specificity (sum of idf), then cheapest": lambda i: (tier[i], -score[i], dur[i]),
    "B  cheapest first inside the tiers": lambda i: (tier[i], dur[i]),
    "C  specificity per second (score / time)": lambda i: (tier[i], -score[i] / max(dur[i], 1e-3)),
    "D  tests of RARE changed entries first, then cheapest": lambda i: (tier[i], 0 if rare[i] > 0 else 1, dur[i]),
    "-  no map: the default order (by class and name)": lambda i: (ix.tests[i][3], ix.tests[i][2]),
}
print(f"  tests executing a RARE changed entry (df <= {RARE:.0f}): {sum(1 for i in ix.conf if rare[i] > 0)}")
for label, key in ORDERS.items():
    order = sorted(ix.conf, key=key)
    cum = 0.0
    pos = {}
    for k, i in enumerate(order):
        for r in RED:
            if r in ix.tests[i][2] and r not in pos:
                pos[r] = (k + 1, cum)
        cum += dur[i]
    first = min(pos.values(), key=lambda v: v[1]) if pos else None
    print(f"{label}")
    for r in RED:
        k, c = pos[r]
        print(f"     {r.split('.')[1][:48]:48s} position {k:5d} of {N}; {c:7.0f} test-s ahead = {100 * c / total:5.2f}%")
    if first:
        print(f"     FIRST RED after {first[1]:.0f} test-s = {100 * first[1] / total:.2f}% of the assembly's work")
