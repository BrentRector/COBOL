"""Are display names a STABLE key for the order plan? What an ADDED golden does to leg 1 (the PB1708 pivot, §3.14.2).

    python b7.py <conformance.trx> <unit.trx> > b7.txt

INPUTS NOT IN THE REPOSITORY: a battery's `conformance.trx` and `unit.trx` (`scripts/battery.sh` PHASE 1; the
committed b7.txt: battery #87, 2026-09-27, run in the worktree `E:\\COBOL-wt\\battery87`). The corpus manifests are
read from THIS checkout (`tests/conformance/*/manifest.json`), so the row ORDER is today's; a row the battery did not
run is charged the median time of its theory.

A partitioned family (`TestPartitioning.Slice`) puts row i in partition class `_P{i % Partitions}`, and the class is
part of every row's display name. Appending one golden to a manifest shifts every LATER row of the family by one
position, so each changes class and therefore NAME: to a plan keyed on raw names they are all UNKNOWN, which puts them
in tier 0 and so in leg 1, outside the 2 % budget. This script measures, per manifest a golden is appended to:
  - how many rows are renamed, and their recorded test-seconds;
  - leg 1 under RAW keys (tier 0 = the renamed rows + the new golden) and under NORMALIZED keys (the partition
    suffix stripped: tier 0 = the new golden alone), with the 2 % cheapest-first budget of b5;
  - leg 1's serial floor: the largest single collection's part (xunit runs one collection serially);
  - leg 1 with a per-collection cap C on the budgeted part (tier 0 is exempt): the barrier idles the other cores
    only while leg 1's longest collection is still running, so a cap bounds the green-path cost of the barrier.
And, per assembly, how many display names embed an absolute path (such a name never matches across worktrees).
"""
import json
import re
import statistics
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path

NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
REPO = Path(__file__).resolve().parents[4]
PARTITIONS = 3            # CorpusRunnerTestsBase.Partitions
BUDGET = 0.02             # LEG_ONE_BUDGET
CAPS = (None, 15.0, 8.0)  # per-collection cap on leg 1's budgeted part, in test-seconds
PART = re.compile(r"_P\d+(?=\.[^.(]+(\(|$))")
ABS = re.compile(r"[A-Za-z]:\\\\|[A-Za-z]:\\|[A-Za-z]:/")


def secs(d):
    h, m, s = d.split(":")
    return int(h) * 3600 + int(m) * 60 + float(s)


def load(trx):
    return [(r.get("testName"), secs(r.get("duration", "0:0:0")))
            for r in ET.parse(trx).getroot().iter(NS + "UnitTestResult")]


def cls_of(name):
    return name.split("(")[0].rsplit(".", 2)[-2]


def normalize(name):
    return PART.sub("", name)


conf = load(sys.argv[1])
unit = load(sys.argv[2])
for label, rows in (("Conformance", conf), ("Unit", unit)):
    paths = [(n, d) for n, d in rows if ABS.search(n)]
    print(f"{label}: {len(rows)} tests; {len(paths)} display names embed an absolute path "
          f"({sum(d for _, d in paths):.1f} test-s)"
          + (f", e.g. {paths[0][0][:150]}" if paths else ""))

# ── the corpus families in today's manifest order ──
pos_t = {}
neg_t = {}
for n, d in conf:
    if ".CorpusRunnerTests_P" not in n:
        continue
    args = re.findall(r'"([^"]*)"', n)
    if "EnabledProgram_" in n:
        pos_t[tuple(args)] = d
    elif "EnabledNegativeCase_" in n:
        neg_t[tuple(args)] = d
med_pos = statistics.median(pos_t.values())
med_neg = statistics.median(neg_t.values())


def manifest(ed):
    return json.loads((REPO / "tests" / "conformance" / ed / "manifest.json").read_text(encoding="utf-8"))["enabled"]


EDS = ["85", "2002", "2014", "2023"]
POS = "EnabledProgram_CompilesStrict_AndMatchesOutIfPresent"
NEG = "EnabledNegativeCase_RejectsWithItsDiagnostic"


def positive_rows(extra_in=None):
    out = []
    for ed in EDS:
        out += [(ed, n) for n in manifest(ed)]
        if extra_in == ed:
            out.append((ed, "NEW-GOLDEN"))
    return out + [("shell", "sentinel")]


def negative_rows(extra=False):
    return [(n,) for n in manifest("negative")] + ([("NEW-GOLDEN",)] if extra else []) + [("sentinel",)]


def corpus_names(prows, nrows):
    """(raw display name, test-s, collection) for every corpus row under the stride."""
    out = []
    for i, a in enumerate(prows):
        args = ", ".join(f'{k}: "{v}"' for k, v in zip(("edition", "name"), a))
        c = f"CorpusRunnerTests_P{i % PARTITIONS}"
        out.append((f"CobolNet.Tests.Conformance.{c}.{POS}({args})", pos_t.get(a, med_pos), c))
    for i, a in enumerate(nrows):
        c = f"CorpusRunnerTests_P{i % PARTITIONS}"
        out.append((f'CobolNet.Tests.Conformance.{c}.{NEG}(name: "{a[0]}")', neg_t.get(a, med_neg), c))
    return out


others = [(n, d, cls_of(n)) for n, d in conf if ".CorpusRunnerTests_P" not in n]
before = corpus_names(positive_rows(), negative_rows())
known_raw = {n for n, _, _ in before + others}
known_norm = {normalize(n) for n in known_raw}
total = sum(d for _, d, _ in before + others)
print(f"corpus rows today: {len(before)} ({sum(d for _, d, _ in before):.0f} test-s); "
      f"assembly {len(before) + len(others)} rows, {total:.0f} test-s; budget {100 * BUDGET:.0f} % = {BUDGET * total:.0f} test-s")


def leg_one(rows, known, key, cap):
    tier0 = [r for r in rows if key(r[0]) not in known]
    rest = sorted((r for r in rows if key(r[0]) in known), key=lambda r: r[1])
    leg, acc, per = list(tier0), 0.0, defaultdict(float)
    for n, d, c in tier0:
        per[c] += d
    budget_per = defaultdict(float)
    for n, d, c in rest:
        if acc + d > BUDGET * total:
            break
        if cap is not None and budget_per[c] + d > cap:
            continue
        leg.append((n, d, c))
        acc += d
        budget_per[c] += d
        per[c] += d
    big = max(per.items(), key=lambda kv: kv[1])
    return len(tier0), sum(d for _, d, _ in tier0), len(leg), sum(d for _, d, _ in leg), big


print()
print("appended to   renamed rows (test-s) | key         cap    tier0 (test-s)   leg 1 (test-s)   floor: largest collection part")
for target in EDS + ["negative"]:
    after = corpus_names(positive_rows(target if target != "negative" else None), negative_rows(target == "negative"))
    rows = after + others
    renamed = [r for r in after if r[0] not in known_raw and "NEW-GOLDEN" not in r[0]]
    first = True
    for keyname, key, known in (("raw", lambda n: n, known_raw), ("normalized", normalize, known_norm)):
        for cap in CAPS:
            t0, t0s, n1, s1, big = leg_one(rows, known, key, cap)
            lead = f"{target:12s} {len(renamed):5d} ({sum(d for _, d, _ in renamed):6.0f})   " if first else " " * 37
            first = False
            print(f"{lead}| {keyname:10s} {('-' if cap is None else f'{cap:.0f}s'):>5s} {t0:6d} ({t0s:6.0f})  "
                  f"{n1:6d} ({s1:6.0f})   {big[1]:6.1f} s ({big[0]})")
