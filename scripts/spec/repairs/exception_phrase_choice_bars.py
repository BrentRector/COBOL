#!/usr/bin/env python3
"""Restore the §5.2.6.4 CHOICE INDICATOR bars to the eight exception-phrase figures that lost them.

THE DEFECT, and why it is normative. Every COBOL statement that can fail prints its handler pair inside ONE tall
bracket carrying a pair of vertical bars:

    ⎡|  ON SIZE ERROR imperative-statement-1      |⎤
    ⎣|  NOT ON SIZE ERROR imperative-statement-2  |⎦

Per §5.2.6.4 those bars mean ZERO OR MORE of the enclosed alternatives, each at most once, in any order — so one
statement may carry BOTH handlers. Per §5.2.6.2 a bracket WITHOUT bars means at most one. Dropping the bars
therefore forbids `COMPUTE ... ON SIZE ERROR ... NOT ON SIZE ERROR ... END-COMPUTE`, which is legal COBOL that
every real program writes. This is the falsely-restrictive direction, and a grammar written from the figure
inherits the restriction.

VERIFIED BY MEASUREMENT, not by eye. `scripts/spec/figure_geometry.py` reads the PDF's vector rectangles — the
brackets and bars are drawn objects, so this is immune to the obfuscated text layer that forces everything else
through a render. On every page repaired here the printed geometry is unambiguous, e.g. page 632:

    x= 76.29  h=31.98  feet at both ends  -> '[' bracket stem
    x= 81.19  h=37.58  no feet, taller    -> '|' choice indicator
    x=304.36  h=37.58  no feet, taller    -> '|' choice indicator
    x=309.72  h=31.98  feet at both ends  -> ']' bracket stem

A bracket stem carries short horizontal feet; a choice-indicator bar is a bare rule, drawn slightly taller. The
tool reports `[ | | ]` for all eight groups repaired here.

THE CLASS WAS ENUMERATED BEFORE ANY EDIT. Of the 30 exception-phrase figure sites in the file, 21 already used the
house style and 9 had lost it. Eight are repaired here; the ninth (ACCEPT Format 3, page 607) is LaTeX rather than
ASCII art and its two independent verifiers disagree about the bracketing of the LINE/COLUMN rows, so it is
handled separately rather than guessed at.

The lost bars had degraded into three different wrong shapes, which is why no single find-and-replace would have
found them all: doubled corner glyphs (`⌈⌊ … ⌉⌋`, `⌐⌐ … ¬¬`), a bracket drawn with bare verticals whose bars were
simply deleted (`⌐ … ¬` over `| … |`), and nested plain brackets (`[ [ … ] ]`).

    python scripts/spec/repairs/exception_phrase_choice_bars.py --dry-run
    python scripts/spec/repairs/exception_phrase_choice_bars.py
"""
from __future__ import annotations

import argparse
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[3]
SPEC = REPO / "specs" / "ISO_COBOL.md"

TOP, BOT = "⎡|", "⎣|"
TOPR, BOTR = "|⎤", "|⎦"


def rows(indent: str, a: str, b: str) -> list[str]:
    """Render one exception-phrase group in the house style, padded so the closing bars line up."""
    w = max(len(a), len(b))
    return [f"{indent}{TOP}  {a.ljust(w)}  {TOPR}", f"{indent}{BOT}  {b.ljust(w)}  {BOTR}"]


def note(stmt: str, a: str, b: str, extra: str = "") -> str:
    return (
        f"> **Figure notes (ISO 5.2.6.4).** The `{a}` / `{b}` group is enclosed in BRACKETS WITH CHOICE "
        f"INDICATORS (`⎡|` … `|⎤`) in the printed standard. Per 5.2.6.4 that means zero or more of the enclosed "
        f"alternatives may be specified, each at most once, in any order — so one {stmt} statement may specify "
        f"neither phrase, `{a}` alone, `{b}` alone, or **both, in either order**. A plain bracket without the "
        f"bars would wrongly read as at-most-one." + (f" {extra}" if extra else "")
    )


# Each site: (label, unique anchor for the first old line, how many old lines to consume, first phrase, second
# phrase, statement name for the note, expected number of occurrences).
SITES = [
    ("p632 COMPUTE Format 1", "⌈⌊ ON SIZE ERROR imperative-statement-1", 2,
     "ON SIZE ERROR imperative-statement-1", "NOT ON SIZE ERROR imperative-statement-2", "COMPUTE", 1),
    ("p635 DELETE Format 1", "⌐ INVALID KEY imperative-statement-1", 2,
     "INVALID KEY imperative-statement-1", "NOT INVALID KEY imperative-statement-2", "DELETE", 1),
    ("p635 DELETE Format 2", "⌐ ON EXCEPTION imperative-statement-3", 2,
     "ON EXCEPTION imperative-statement-3", "NOT ON EXCEPTION imperative-statement-4", "DELETE FILE", 1),
    ("p645 DIVIDE Formats 4 and 5", "⌐⌐ ON SIZE ERROR imperative-statement-1", 2,
     "ON SIZE ERROR imperative-statement-1", "NOT ON SIZE ERROR imperative-statement-2", "DIVIDE", 2),
    ("p722 READ Format 1", "[ [ AT END imperative-statement-1", 2,
     "AT END imperative-statement-1", "NOT AT END imperative-statement-2", "READ", 1),
    ("p722 READ Format 2", "[ [ INVALID KEY imperative-statement-3", 2,
     "INVALID KEY imperative-statement-3", "NOT INVALID KEY imperative-statement-4", "READ", 1),
    ("p799 UNSTRING", "| |  ON OVERFLOW imperative-statement-1", 2,
     "ON OVERFLOW imperative-statement-1", "NOT ON OVERFLOW imperative-statement-2", "UNSTRING", 1),
]

# The UNSTRING group additionally carries a mangled corner row above and below the two text rows; those rows are
# the broken bracket itself and are consumed with it.
UNSTRING_STRAY = ("⌐ ⌐", "L L")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    lines = SPEC.read_text(encoding="utf-8").splitlines()

    # Locate every target FIRST, then edit strictly back-to-front, so that a note inserted for one site cannot
    # shift the line numbers of another. Notes are bound to the positions actually repaired — matching on phrase
    # text alone would also hit the ALREADY-CORRECT figures (ADD, MULTIPLY and SUBTRACT print the very same
    # "ON SIZE ERROR imperative-statement-1" row) and bolt redundant notes onto healthy transcriptions.
    targets = []
    for label, anchor, span, a, b, stmt, expect in SITES:
        idxs = [i for i, l in enumerate(lines) if anchor in l]
        if len(idxs) != expect:
            sys.exit(f"FATAL: {label}: expected {expect} occurrence(s) of {anchor!r}, found {len(idxs)}")
        for i in idxs:
            lo, hi = i, i + span
            # UNSTRING's broken bracket spans two extra rows; absorb them so no orphan corners survive.
            if UNSTRING_STRAY[0] in lines[i - 1] and UNSTRING_STRAY[1] in lines[i + span]:
                lo, hi = i - 1, i + span + 1
            if not any(b in l for l in lines[lo:hi]):
                sys.exit(f"FATAL: {label}: the second alternative {b!r} is not inside the consumed range")
            targets.append((lo, hi, label, a, b, stmt))

    total = added = 0
    for lo, hi, label, a, b, stmt in sorted(targets, reverse=True):
        indent = re.match(r"^\s*", lines[lo]).group(0)
        lines[lo:hi] = rows(indent, a, b)
        total += 1
        fence = next((j for j in range(lo, min(lo + 14, len(lines))) if lines[j].strip() == "```"), None)
        if fence is None:
            sys.exit(f"FATAL: {label}: no closing fence found after the repaired group")
        if any("5.2.6.4" in lines[k] for k in range(fence, min(fence + 4, len(lines)))):
            print(f"  {label:<32} lines {lo + 1}-{hi} -> house style (note already present)")
            continue
        lines[fence + 1:fence + 1] = ["", note(stmt, a, b)]
        added += 1
        print(f"  {label:<32} lines {lo + 1}-{hi} -> house style + figure note")

    print(f"\n{total} group(s) restored, {added} figure note(s) added")
    if total != 8:
        sys.exit(f"FATAL: expected to restore exactly 8 groups, restored {total}")
    if args.dry_run:
        print("DRY RUN — nothing written.")
        return 0

    text = "\n".join(lines) + "\n"
    SPEC.write_text(text, encoding="utf-8", newline="\n")

    # Post-write assertions: none of the broken shapes may survive anywhere in the file.
    for shape in ("⌈⌊", "⌉⌋", "⌐⌐", "¬¬", "⌊⌊", "┘┘"):
        if shape in text:
            sys.exit(f"FATAL: the degraded delimiter {shape!r} still appears in the file")
    for anchor in ("[ [ AT END", "[ [ NOT AT END", "[ [ INVALID KEY", "[ [ NOT INVALID KEY"):
        if anchor in text:
            sys.exit(f"FATAL: the nested-bracket rendering {anchor!r} still appears in the file")
    print(f"wrote {SPEC.relative_to(REPO)} — no degraded delimiter shapes remain")
    return 0


if __name__ == "__main__":
    sys.exit(main())
