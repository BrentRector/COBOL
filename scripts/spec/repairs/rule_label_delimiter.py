#!/usr/bin/env python3
"""Restore the printed delimiter of a numbered rule whose label was transcribed `N.` at column 0 (kb/Work PB1635).

THE DEFECT. The transcription writes every top-level rule label as `N\\)` (see rule_numbering.py — the standard
prints `1)` for rules, `a)` for their sub-items and `1.` for the THIRD level). 107 labels were transcribed `N.` at
column 0 instead. `cite.py`'s TOP pattern and the rule-catalog segmenter both key on the delimiter form, so a
rule whose label is wrong is answered with the wrong rule path — `cite.py --check 8.8.3.3 "equivalent to a
literal"` named `c)`, the last sub-item of General rule 1, for what is General rule 3.

HOW THE POPULATION IS DECIDED: BY MEASUREMENT, NEVER BY SHAPE. Every column-0 `N.` line of the body is matched to
its printed line in the PDF, in reading order within its clause's own pages, and the delimiter the PDF prints after
the ordinal is what decides:

    printed `N)`  ->  the transcription is wrong; rewritten `N\\)`                        (107 lines, this script)
    printed `N.`  ->  the label is right, the INDENTATION is lost (a third-level item printed at the third level
                      but transcribed at column 0). 152 lines. LEFT ALONE: indenting them turns each nested list
                      into a fold of its parent rule in the catalog, which is the PB55 "THE RULE IS THE UNIT" wave
                      the owner scheduled for after the defect burn-down (extract_rule_catalog.py, INDENTED_ORDINAL).

A line the PDF match cannot decide is REPORTED and never guessed at. The Addendum's own numbered lists are the
publisher's text, not the standard's, and are outside the measured region.

NEEDS THE PDF (the private submodule) — it measures the printed page. Idempotent: a second run finds no `)`-printed
line at column 0 and changes nothing.

    python scripts/spec/repairs/rule_label_delimiter.py            # report
    python scripts/spec/repairs/rule_label_delimiter.py --apply
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[2]
sys.path.insert(0, str(HERE.parent))
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"

HEADING = re.compile(r"^#{2,6}\s+([0-9]+(?:\.[0-9]+)*|[A-Z](?:\.[0-9]+)+)(?:\s+(.*?))?\s*$")
DOT_LABEL = re.compile(r"^(\d+)\.(\s+)(.*)$")
PRINTED_MARK = re.compile(r"^\s*(\d{1,3})\s*([.)])\s*(.*)$")


def words_key(text: str) -> str:
    """Letters and digits only, lower case — the comparison key that survives every typographic difference."""
    return re.sub(r"[^a-z0-9]+", "", text.lower())


def printed_markers(doc):
    """first-10-letters key -> [(pdf_page, y, ordinal, printed delimiter, 25-letter key)] per numbered PDF line.

    In the PDF the ordinal and its text are frequently TWO lines at the same height (the ordinal is its own text
    run), so a bare marker takes its text from the next run on that row."""
    index = collections.defaultdict(list)
    for pno in range(doc.page_count):
        raw = doc[pno].get_text("dict")
        runs = [("".join(s["text"] for s in ln["spans"]), ln["bbox"][0], ln["bbox"][1])
                for b in raw["blocks"] for ln in b.get("lines", [])]
        runs.sort(key=lambda r: (round(r[2]), r[1]))
        for k, (text, x0, y0) in enumerate(runs):
            if not (m := PRINTED_MARK.match(text)):
                continue
            rest = m.group(3)
            if not rest.strip():
                for t2, x2, y2 in runs[k + 1:k + 3]:
                    if abs(y2 - y0) < 3 and x2 > x0:
                        rest = t2
                        break
            index[words_key(rest)[:10]].append((pno + 1, y0, int(m.group(1)), m.group(2), words_key(rest)[:25]))
    return index


def classify(lines, doc):
    """[(line_index, ordinal, printed delimiter or None)] for every column-0 `N.` line of the body."""
    import clause_page                                   # noqa: E402  (path injected above)
    pages = {}
    headings = clause_page.index(doc)
    for k, h in enumerate(headings):
        end = headings[k + 1].page if k + 1 < len(headings) else h.page
        pages.setdefault(h.clause, (h.page, end))
    index = printed_markers(doc)

    out, clause, last = [], None, {}
    in_pre = in_fence = False
    for i, line in enumerate(lines):
        s = line.strip()
        if 'id="addendum"' in line:
            break                                        # the Addendum is the publisher's text, not the standard's
        if s.startswith("<pre"):
            in_pre = True
        elif s == "</pre>":
            in_pre = False
        elif s.startswith("```"):
            in_fence = not in_fence
        if m := HEADING.match(line):
            clause = m.group(1)
            continue
        if in_pre or in_fence or not (m := DOT_LABEL.match(line)):
            continue
        n, key = int(m.group(1)), words_key(m.group(3))[:25]
        # a printed line may WRAP earlier than the transcription's, so either key may be the prefix of the other
        cand = [c for c in index.get(key[:10], []) if c[2] == n and (key.startswith(c[4]) or c[4].startswith(key))]
        if (span := pages.get(clause)) and len(cand) > 1:
            cand = [c for c in cand if span[0] <= c[0] <= span[1]] or cand
        if not cand:                                     # a wrapped or re-flowed first line: try the heading's own pages
            out.append((i, n, None))
            continue
        after = last.get(clause, (0, 0.0))
        pick = next((c for c in cand if (c[0], c[1]) > after), cand[0])
        last[clause] = (pick[0], pick[1])
        out.append((i, n, pick[3]))
    return out


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    import clause_page
    import fitz
    doc = fitz.open(clause_page.find_pdf())

    lines = SPEC_MD.read_text(encoding="utf-8").split("\n")
    verdicts = classify(lines, doc)
    tally = collections.Counter(d or "undecided" for _, _, d in verdicts)
    fix = [i for i, _, d in verdicts if d == ")"]
    print(f"{len(verdicts):5}  column-0 `N.` lines in the body")
    print(f"{tally[')']:5}  printed `N)` -> rewritten `N\\)` by this script")
    print(f"{tally['.']:5}  printed `N.` -> a third-level item whose INDENTATION is lost; left alone (PB55 wave)")
    print(f"{tally['undecided']:5}  not decidable by the PDF match")
    for i, n, d in verdicts:
        if d is None:
            print(f"         undecided: line {i + 1}: {lines[i][:90]}")
    if not args.apply:
        print("\n(report only — pass --apply to write)")
        return 0
    for i in fix:
        m = DOT_LABEL.match(lines[i])
        lines[i] = f"{m.group(1)}\\){m.group(2)}{m.group(3)}"
    SPEC_MD.write_text("\n".join(lines), encoding="utf-8")
    print(f"applied: {len(fix)} labels in {SPEC_MD.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
