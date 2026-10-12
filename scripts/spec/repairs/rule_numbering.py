#!/usr/bin/env python3
"""De-list the standard's rule hierarchy — `1)` / `a)` / `1.` are LABELS, not list positions.

WHAT WAS WRONG. Every top-level rule was written as `1) …` at column 0, which CommonMark parses as an ORDERED
LIST ITEM. Three things follow, and only the first is cosmetic:

  1. THE MARKER CHANGES. HTML `<ol>` has no per-item delimiter, so the printed `1)` renders as `1.` — and the
     standard prints `1.` for its THIRD level (see the printed page: `1)` → `a)` → `1.`). A top-level rule and
     a third-level sub-item therefore become indistinguishable in the rendered document.
  2. THE DOCUMENT CONTRADICTS ITSELF. `a)` is not a Markdown marker, so the SECOND level always rendered as
     the literal `a)` it is printed as. One structure, two renderings, in the same clause.
  3. ⛔ CONTENT IS DESTROYED. A list item's content is re-parsed as Markdown BLOCK syntax, so a rule beginning
     with `>` opens a blockquote and the `>` is EATEN:
         `3) >>IF conditional-expression-1 shall begin…`  → renders "IF conditional-expression-1 shall begin…"
         `9) >= is an abbreviation for GREATER THAN OR EQUAL TO.` → renders "= is an abbreviation for GREATER
            THAN OR EQUAL TO." — which is a FALSE normative statement about `=`.
     Eight rules were affected: six compiler directives (>>IF/>>ELSE/>>END-IF/>>WHEN/>>END-EVALUATE) and two
     relational-operator definitions in §8.7.5.2. A whole-document sweep found `>` to be the ONLY block
     syntax that bites — no rule begins with `#`, `-`, a fence or a pipe — so de-listing is what fixes the
     corruption AND the marker at once, rather than escaping eight `>` and leaving the rest wrong.

WHAT THIS DOES. Escapes the marker so the line is an ordinary paragraph carrying the printed label, and
repairs the two shapes that only survived because they sat INSIDE a list item:

    `1) text`          → `1\\) text`        the label, as printed (4,161)
    `    a) text`      → `   a) text`      4 spaces outside a list is a CODE BLOCK (17)
    `      1. text`    → `   1\\. text`     same, plus the label (93 at 6 spaces, 6 at 3)

`a)` at 0 or 3 spaces is already correct and is not touched. `1.` at column 0 already renders as the printed
`1.` and is left alone — it is its own list either way, since CommonMark starts a new list on a changed
delimiter.

    python scripts/spec/repairs/rule_numbering.py            # report
    python scripts/spec/repairs/rule_numbering.py --apply
"""
from __future__ import annotations

import argparse
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[3]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"

# The three edits, each keyed on the shape MEASURED in the file (see the module docstring for the counts).
# Order matters only in that each pattern is anchored, so they cannot overlap.
EDITS = (
    ("L1 rule label",       re.compile(r"^(\d+)\)(\s)"),        r"\1\\)\2"),
    ("L2 sub-rule indent",  re.compile(r"^ {4,}([a-z])\)(\s)"), r"   \1)\2"),
    ("L3 sub-item",         re.compile(r"^ {3,}(\d+)\.(\s)"),   r"   \1\\.\2"),
)


def repair(lines):
    """Rewrite the rule labels outside every verbatim region, and report what changed.

    ⚠ A `<pre>` block or a fence is VERBATIM — a figure's own text may begin with a digit and a paren, and
    rewriting one would corrupt a general format. The same mask the rest of the tooling uses."""
    out, counts, inpre, infence = [], {name: 0 for name, _, _ in EDITS}, False, False
    for line in lines:
        s = line.strip()
        if s.startswith("<pre"):
            inpre = True
        elif s == "</pre>":
            inpre = False
        elif s.startswith("```"):
            infence = not infence
        if not inpre and not infence:
            for name, pat, repl in EDITS:
                new = pat.sub(repl, line, count=1)
                if new != line:
                    counts[name] += 1
                    line = new
                    break
        out.append(line)
    return out, counts


def words(text):
    """Every word the reader sees, with the label punctuation normalised away — the conservation check.

    The escape adds a backslash the RENDERER removes, so comparing raw text would report 4,000 differences
    that do not exist on the page. Dropping every backslash first — the same transform on both sides — and
    splitting on whitespace compares what a reader ends up with, which is the whole-artifact invariant that
    caught two duplication bugs in the figure sweep. Indentation changes wash out with the whitespace."""
    return text.replace("\\", "").split()


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    src = SPEC_MD.read_text(encoding="utf-8")
    lines = src.splitlines()
    out, counts = repair(lines)

    before, after = words(src), words("\n".join(out))
    if before != after:
        sys.exit(f"FATAL: word conservation failed — {len(before)} words in, {len(after)} out. "
                 f"The repair may only re-punctuate a label; it may never change a word.")

    for name, n in counts.items():
        print(f"{n:6}  {name}")
    print(f"{sum(counts.values()):6}  TOTAL — words conserved ({len(before):,})")
    if not args.apply:
        print("\n(report only — pass --apply to write)")
        return 0
    SPEC_MD.write_text("\n".join(out) + "\n", encoding="utf-8", newline="\n")
    print(f"applied to {SPEC_MD.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
