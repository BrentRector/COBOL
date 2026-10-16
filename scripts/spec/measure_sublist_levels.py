#!/usr/bin/env python3
"""Measure, on the printed PDF, the LEVEL of every catalog sub-list — and record it as data the extractor checks.

WHY A SUB-LIST NEEDS A MEASUREMENT (kb/Work PB55, owner decision 2026-08-08 "THE RULE IS THE UNIT"). A rule
block's catalog rows are its TOP-LEVEL numbered rules; a numbered list printed INSIDE a rule — "GR 9 c) 1. 2. 3."
— is that rule's body, addressed by citation path, never a row. `extract_rule_catalog.py` tells the two apart by
the transcription's indentation, and the transcription flattened 51 such nested lists onto column 0, where each
became an `L<sublist>.<n>` row of its own: 152 sub-item FRAGMENTS in the denominator, decided by a transcriber's
indentation, not by the standard. Indentation in the markdown cannot settle which column-0 list is which — a
nested list flattened at a page break and a list the standard really prints at the margin look the same — so the
answer is a MEASUREMENT of the printed page, like `measure_index_levels.py`'s.

WHAT IS MEASURED. For each sub-list (catalog rows with `sublist` > 1) the printed line of its OPENING item is
located on the clause's pages and its ordinal glyph's x0 is set against that page's own left margin: the body
margin is the leftmost line start in the body band (63.49 pt on an even page, 80.04 pt on an odd one), and each
nesting level sits 17.5 pt further right (`a)` at +17.5, a list under `a)` at +35). Level 0 is a list the
standard prints at the block's own margin — a PEER list, introduced by its own prose lead-in ("The size error
condition exists in the following cases:", §14.7.5), whose items are rules in their own right. Anything deeper
is a flattened nested list and must be indented in `specs/ISO_COBOL.md`, never recorded.

`--write` records the measurement in `scripts/spec/data/sublist-levels.json`, which is committed so that
`extract_rule_catalog.py --check` can hold every sub-list to it in the public repository and in CI, where the
licensed PDF is not available: a sub-list nobody measured, or one measured deeper than level 0, FAILS the check.

    python scripts/spec/measure_sublist_levels.py            # report
    python scripts/spec/measure_sublist_levels.py --write    # record the measurement
"""
from __future__ import annotations

import argparse
import collections
import json
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO / "scripts" / "spec"))
from cite import norm  # noqa: E402  — ONE notion of "the same words" as the citation checker's

PDF = next(iter(sorted((REPO / "specs-private").glob("*COBOL*.pdf"))), None)
CATALOG = REPO / "docs" / "rearchitecture" / "spec-rule-catalog.json"
DATA = REPO / "scripts" / "spec" / "data" / "sublist-levels.json"

LEVEL_PT = 17.5            # one nesting step on the printed page (a) under 1), 1. under a))
HEAD_Y, FOOT_Y = 60, 760   # the running head and the per-copy licence footer
PAGES_PER_CLAUSE = 10      # how far past a clause's heading page its rule text can run
KEY_WORDS = 100            # how much of the opening item's normalized text keys a sub-list


def sublist_key(section: str, text: str) -> str:
    """The key a sub-list is recorded under: its clause and the start of its opening item's words.

    Positional ids (`…-L<sublist>.<n>`) shift whenever a list above them is re-levelled (kb/Work PB698), so the
    key is the TEXT; `extract_rule_catalog.py` computes the same key with this same function."""
    return f"{section}|{norm(text.replace(chr(92), ''))[:KEY_WORDS]}"


def openings(rules: list[dict]) -> list[dict]:
    """The opening rule of every sub-list, in catalog order."""
    seen: set[tuple[str, str, int]] = set()
    out = []
    for r in rules:
        k = (r["kind"], r["section"], r["sublist"])
        if r["sublist"] > 1 and k not in seen:
            seen.add(k)
            out.append(r)
    return out


def _visual_lines(page) -> list[tuple[float, str]]:
    """(x0, text) of every printed line in the body band, top to bottom."""
    rows: dict[int, list] = collections.defaultdict(list)
    for w in page.get_text("words"):
        if HEAD_Y < w[1] < FOOT_Y:
            rows[round(w[3])].append(w)
    return [(min(w[0] for w in ws), " ".join(w[4] for w in sorted(ws, key=lambda w: w[0])))
            for _, ws in sorted(rows.items())]


def measure(doc, rules: list[dict]) -> list[dict]:
    import clause_page                                             # noqa: E402  (path injected above)

    start = {}
    for h in clause_page.index(doc):
        start.setdefault(h.clause, h.page)
    lines_of: dict[int, list] = {}
    out = []
    for r in openings(rules):
        sec = r["section"]
        want = norm(r["text"].replace(chr(92), ""))
        label = re.compile(rf"^{r['ordinal']}[.)]\s*")
        hits = []
        first = start.get(sec)
        for pno in range(first - 1, min(first - 1 + PAGES_PER_CLAUSE, doc.page_count)) if first else ():
            if pno not in lines_of:
                lines_of[pno] = _visual_lines(doc[pno])
            vis = lines_of[pno]
            margin = min((x for x, _ in vis), default=0.0)
            for x0, text in vis:
                if (m := label.match(text)) and (got := norm(text[m.end():])) and want.startswith(got[:40]) \
                        and len(got) >= min(20, len(want)):
                    hits.append({"pdf_page": pno + 1, "x0": round(x0, 2), "level": round((x0 - margin) / LEVEL_PT)})
        levels = sorted({h["level"] for h in hits})
        out.append({"key": sublist_key(sec, r["text"]), "row-at-measure": r["id"], "text": r["text"][:70],
                    "level": levels[0] if len(levels) == 1 else None,
                    "printed": hits[:3]})
    return out


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--write", action="store_true", help=f"record the measurement in {DATA.relative_to(REPO)}")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if PDF is None or not PDF.exists():
        sys.exit("FATAL: the ISO PDF was not found under specs-private/ (a private submodule: "
                 "git submodule update --init specs-private). This tool measures the printed page.")
    import fitz                                                    # PyMuPDF, as the sibling tools use it

    rules = json.loads(CATALOG.read_text(encoding="utf-8"))["rules"]
    with fitz.open(PDF) as doc:
        measured = measure(doc, rules)
    bad = [m for m in measured if m["level"] != 0]
    for m in measured:
        flag = "ok   " if m["level"] == 0 else "⛔   "
        print(f"{flag}{m['row-at-measure']:<22} level {m['level']}  {m['printed'][:1]}  {m['text'][:50]}")
    print(f"\n{len(measured)} sub-list(s) measured; {len(measured) - len(bad)} printed at the block's own margin")
    if bad:
        print(f"⛔ {len(bad)} sub-list(s) NOT measured at level 0 — a flattened nested list (indent it in "
              "specs/ISO_COBOL.md at its printed level) or a line this tool could not find (read the page).")
    if args.write:
        if bad:
            print("NOT WRITTEN: only lists measured at the margin are recorded.")
            return 1
        DATA.write_text(json.dumps({
            "$comment": ["Written by scripts/spec/measure_sublist_levels.py --write; never edit by hand (kb/Work PB55).",
                         "Every catalog sub-list (an L<sublist>.<n> row family) MEASURED on the printed PDF at its rule",
                         "block's own margin: a peer list with its own lead-in, whose items are rules. "
                         "extract_rule_catalog.py --check fails on a sub-list this file does not record."],
            "sublists": [{k: m[k] for k in ("key", "row-at-measure", "level", "printed")} for m in measured],
        }, indent=1, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
        print(f"wrote {DATA.relative_to(REPO)}")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
