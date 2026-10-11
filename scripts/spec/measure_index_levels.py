#!/usr/bin/env python3
"""Measure the LEVEL of every printed index entry, and emit it as data for `relist_index.py`.

WHY THIS IS DATA AND NOT A HEURISTIC. `relist_index.py` can infer most lost sub-entries from alphabetical
order — inside `### B`, an entry that does not begin with B cannot be a top-level term. That inference is
right 2,485 times out of 2,503 checked, and it is structurally blind to the rest: a sub-entry labelled
`Definition` sitting in section D begins with D, so nothing about the letter gives it away. `Definition`
appears 75 times in the printed index and is a sub-entry every single time.

The printed page states the level directly — a sub-entry is set about 18 pt further right than its term — so
the answer is a MEASUREMENT, not an inference. This script takes it and writes it to
`scripts/spec/data/index-levels.json`, which is committed so that `relist_index.py` stays reproducible in the
public repo, where the licensed PDF is not available.

ONLY UNAMBIGUOUS ENTRIES ARE RECORDED. Index entry text repeats — 75 `Definition`s, 7 `operator`s — so a key
cannot identify WHICH printed line an entry came from. That does not matter when every printed instance of
that text carries the SAME level, which is the usual case; a key whose instances disagree is omitted and left
to the heuristic.

The index is set in two columns per page, each with its own left edge, so a level is "which of the two edges
of MY column am I at" and never an absolute x.

    python scripts/spec/measure_index_levels.py            # report
    python scripts/spec/measure_index_levels.py --write    # update the committed data
"""
from __future__ import annotations

import argparse
import collections
import json
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
PDF = next(iter(sorted((REPO / "specs-private").glob("*COBOL*.pdf"))), None)
DATA = REPO / "scripts" / "spec" / "data" / "index-levels.json"

SUB_INDENT_PT = 8          # a sub-entry sits well beyond this; wrapped continuation lines sit further still
HEAD_Y, FOOT_Y = 60, 740   # the running head and the per-copy licence footer
COLUMN_SPLIT = 300


def entry_key(text: str) -> str:
    """A comparable form of an entry: its words, without links, page folios or punctuation noise."""
    text = re.sub(r"^\s*-\s+", "", text)
    text = re.sub(r"\[[^\]]*\]\([^)]*\)", " ", text)      # markdown links
    text = re.sub(r"[\d,\s]+$", "", text)                 # printed page folios
    text = re.sub(r"[^A-Za-z0-9 ()\-.,]", " ", text)
    return " ".join(text.split()).lower()[:40]


def measure(doc):
    first = None
    for pno in range(doc.page_count - 80, doc.page_count):
        if "BIBLIOGRAPHY" in doc[pno].get_text():
            first = pno + 1
    if first is None:
        sys.exit("FATAL: the bibliography was not found, so the index cannot be located")

    out = []
    for pno in range(first, doc.page_count):
        rows = collections.defaultdict(list)
        for x0, y0, x1, y1, w, *_ in doc[pno].get_text("words"):
            if HEAD_Y < y0 < FOOT_Y:
                rows[(x0 < COLUMN_SPLIT, round(y0, 1))].append((x0, w))
        edges = {}
        for (isleft, _), r in rows.items():
            x = min(x for x, _ in r)
            edges[isleft] = min(edges.get(isleft, x), x)
        for (isleft, y), r in rows.items():
            line = sorted(r)
            text = " ".join(w for _, w in line)
            if re.fullmatch(r"[A-Z]|Symbols|Numerics|INDEX|\d+", text.strip()):
                continue                                   # a letter heading or a bare folio
            out.append((entry_key(text), 0 if line[0][0] - edges[isleft] < SUB_INDENT_PT else 1))
    return first, out


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--write", action="store_true", help="update scripts/spec/data/index-levels.json")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if PDF is None or not PDF.exists():
        sys.exit("FATAL: the ISO PDF was not found under specs-private/. It is licensed per-copy and lives "
                 "in a PRIVATE submodule: git submodule update --init specs-private")
    import fitz

    first, measured = measure(fitz.open(PDF))
    print(f"printed index: PDF pages {first + 1}..")
    print(f"entries measured : {len(measured)} "
          f"({sum(1 for _, l in measured if l == 0)} top-level, {sum(1 for _, l in measured if l == 1)} sub)")

    levels = collections.defaultdict(set)
    for k, lvl in measured:
        levels[k].add(lvl)
    unambiguous = {k: next(iter(v)) for k, v in levels.items() if len(v) == 1 and k}
    print(f"distinct entry texts : {len(levels)}")
    print(f"  unambiguous        : {len(unambiguous)}  (every printed instance at the same level)")
    print(f"  ambiguous, omitted : {len(levels) - len(unambiguous)}  (left to the alphabetical heuristic)")

    if args.write:
        DATA.parent.mkdir(parents=True, exist_ok=True)
        DATA.write_text(json.dumps(dict(sorted(unambiguous.items())), indent=0, ensure_ascii=False),
                        encoding="utf-8", newline="\n")
        print(f"\nwrote {DATA.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
