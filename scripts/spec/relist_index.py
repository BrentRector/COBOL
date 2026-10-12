#!/usr/bin/env python3
"""Rewrite the transcription's index as a real Markdown nested list.

WHY. The index is 3,123 lines of consecutive non-blank text with almost no list markup. Markdown joins
consecutive lines into ONE PARAGRAPH, so the whole of letter B renders as a single run-on block — every term
and every sub-entry flowing together. The links inside it all resolve; the structure around them was never
written. This is the same defect the table of contents had.

THREE THINGS ARE REPAIRED, and only these:

  LIST      A top-level term becomes `- term`; a sub-entry, currently indented with four `&nbsp;` entities,
            becomes a nested `  - sub-entry`. `&nbsp;` is a rendering hack for indentation that a list
            expresses natively, and it is what forced the flat structure in the first place.

  LEVEL     A sub-entry that lost its indentation is recovered from three sources, in this order:

            1. THE MEASURED PRINTED INDENTATION, which is the authority — `data/index-levels.json`, produced
               by `measure_index_levels.py` from the page and committed so this runs without the PDF.
            2. Alphabetical order, for entries the measurement cannot name unambiguously: inside `### B`, a
               column-0 entry that does not begin with B cannot be a top-level term. `Boolean expressions` is
               followed by `COMPUTE statement`, `EVALUATE statement` and `Parenthesis in` at column 0 — all
               sub-entries of it.
            3. In `Symbols` and `Numerics` there is no letter to agree with, but the TERMS are the symbols, so
               a line opening with a letter (`operator`, `Comment line`) is a sub-entry of the symbol above.

            Rules 2 and 3 agree with the printed page 2,485 times out of 2,503 checked. They are structurally
            blind to the other 18 — a sub-entry labelled `Definition` in section D begins with D, and nothing
            about the letter gives it away — which is exactly what rule 1 exists to settle.

  FURNITURE The 30 `---` rules left behind by page breaks. A page is not a thing in Markdown, and inside a
            list a horizontal rule terminates the list.

Letter headings are normalised to `###`, one level below `## Index`. They were written `### A` but `## B`
through `## Z`, which put 25 of the 26 letters at the same level as the index that contains them.

WORD CONSERVATION is the safety gate: this pass may only add list markers and remove `&nbsp;` and page rules,
so every word in the index must survive it. The script refuses to write if any word is lost or gained.

    python scripts/spec/relist_index.py --report
    python scripts/spec/relist_index.py --apply
"""
from __future__ import annotations

import argparse
import collections
import json
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"
LEVELS = REPO / "scripts" / "spec" / "data" / "index-levels.json"

NBSP = re.compile(r"^(?:&nbsp;)+")
LETTER = re.compile(r"^#{2,3}\s+([A-Z]|Symbols|Numerics)\s*$")
# The first word of an entry, ignoring any markup, for the alphabetical test.
FIRST_LETTER = re.compile(r"[A-Za-z]")


def entry_key(text):
    """The same comparable form `measure_index_levels.py` keys its data on."""
    text = re.sub(r"^\s*-\s+", "", text)
    text = re.sub(r"\[[^\]]*\]\([^)]*\)", " ", text)
    text = re.sub(r"[\d,\s]+$", "", text)
    text = re.sub(r"[^A-Za-z0-9 ()\-.,]", " ", text)
    return " ".join(text.split()).lower()[:40]


def escape_term(body):
    """Escape an index term that IS Markdown syntax.

    The Symbols section indexes the language's punctuation, so several of its terms are markup: `>`, `>=` and
    `>>` open a blockquote even inside a list item, and `*` and `**` open emphasis. They must render as the
    characters they name."""
    if body.startswith(">"):
        return "\\" + body
    lead = len(body) - len(body.lstrip("*"))
    return "\\*" * lead + body[lead:] if lead else body


def index_span(lines):
    start = next(i for i, l in enumerate(lines) if l.startswith("## Index"))
    end = next(i for i, l in enumerate(lines) if l.startswith("## Corrections applied"))
    return start, end


def words(text):
    """Every word of the index, ignoring the markup this pass is allowed to change: list bullets, `&nbsp;`
    indentation, and the `#` of a heading whose LEVEL is being normalised."""
    return collections.Counter(re.sub(r"&nbsp;|^#+|[-*\\]", " ", text, flags=re.M).split())


def measured_levels():
    """The level of each printed index entry, measured off the page by `measure_index_levels.py`.

    This is the AUTHORITY where it has an answer. The alphabetical heuristic below is structurally blind to a
    sub-entry whose text begins with its own section's letter — `Definition` in section D is a sub-entry all
    75 times it is printed, and nothing about the letter says so. The data is committed, so this works in the
    public repo without the licensed PDF."""
    if not LEVELS.exists():
        return {}
    return json.loads(LEVELS.read_text(encoding="utf-8"))


def relist(lines):
    out, section, stats = [], None, collections.Counter()
    levels = measured_levels()
    for line in lines:
        s = line.rstrip()
        m = LETTER.match(s)
        if m:
            section = m.group(1)
            out.append(f"### {section}")
            stats["headings"] += 1
            continue
        # A BLOCKQUOTE requires `> ` — the editorial note at the head of the index. `>`, `>=` and `>>` with
        # no space are INDEX TERMS: the relation operators and the directive indicator, which live in the
        # Symbols section. Treating them as blockquotes rendered three symbols as quoted text and orphaned
        # the sub-entries beneath them. (The same `>`-is-not-a-quote trap cost a figure sweep once already.)
        if s.startswith("#") or re.match(r"^>\s", s):
            out.append(s)
            continue
        if s.strip() == "---":
            stats["page_rules_removed"] += 1
            continue
        if not s.strip():
            # A blank between two entries only splits the list; keep blanks around headings and notes.
            if out and (out[-1].startswith("#") or out[-1].startswith(">")):
                out.append("")
            continue
        if NBSP.match(s):
            out.append("  - " + NBSP.sub("", s).strip())
            stats["sub_entries"] += 1
            continue
        body = s.strip()
        # An entry inside a letter section that does not start with that letter is a sub-entry whose
        # indentation was lost.
        first = FIRST_LETTER.search(body)
        sub = (section and len(section) == 1 and first and first.group(0).upper() != section)
        # Symbols and Numerics have no letter to agree with, but their terms ARE the symbols: a line that
        # opens with a letter there (`operator`, `Comment line`, `Relation`) is a sub-entry of the symbol
        # above it. Measured against the printed indentation, this is the whole of that section's structure.
        if section in ("Symbols", "Numerics"):
            sub = bool(body[:1].isalpha())
        # The measurement outranks both: it read the level off the printed page.
        printed = levels.get(entry_key(body))
        if printed is not None:
            if printed == 1 and not sub:
                stats["relevelled_by_measurement"] += 1
            sub = printed == 1
        if sub and out and out[-1].lstrip().startswith("-"):
            out.append("  - " + escape_term(body))
            stats["relevelled"] += 1
            continue
        out.append("- " + escape_term(body))
        stats["top_level"] += 1
    return out, stats


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--report", action="store_true")
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    lines = SPEC_MD.read_text(encoding="utf-8").splitlines()
    a, b = index_span(lines)
    new, stats = relist(lines[a:b])

    for k, v in stats.most_common():
        print(f"  {v:6}  {k}")
    print(f"\n  {b - a:6}  lines before")
    print(f"  {len(new):6}  lines after")

    before, after = words("\n".join(lines[a:b])), words("\n".join(new))
    lost, gained = before - after, after - before
    print(f"\nword conservation: {sum(before.values())} before, {sum(after.values())} after")
    if lost or gained:
        print(f"  ✗ lost {sum(lost.values())}: {list(lost.elements())[:8]}")
        print(f"  ✗ gained {sum(gained.values())}: {list(gained.elements())[:8]}")
        sys.exit("FATAL: the index lost or gained words — this pass may only change list markup")
    print("  ✓ every word survived")

    if args.apply:
        lines[a:b] = new
        SPEC_MD.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
        print(f"\napplied to {SPEC_MD.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
