#!/usr/bin/env python3
"""Remove the horizontal rules left behind by de-paging.

A printed page boundary was transcribed as a block of three things: a `<a id="page-N">` anchor, a running
header, and a pair of `---` separators around them. De-paging removed the first two and left 769 of the third,
so the document is strewn with horizontal rules that used to mark a page boundary and now mark nothing. They
render as lines cutting through the prose, and 287 of them sit in pairs with nothing at all between.

THEY ARE RESIDUE, NOT A CONVENTION, and the document says so itself: of 2,691 headings, 2,380 have no rule
before them. An 11% minority is not a house style.

⚠ THE FRONT MATTER IS LEFT ALONE. Before clause 1 the rules are hand-authored editorial dividers — they
separate the title-page blocks (title, French title, reference number, copyright) and set off the preface's
`About this transcription` note. Those were never page furniture, and a rule that divides two blocks on ONE
printed page is doing a job.

Three invariants, all checked before anything is written: no word changes, no heading is created or destroyed,
and no rule is removed that would turn a line above it into a setext H2.

    python scripts/spec/strip_page_rules.py --report
    python scripts/spec/strip_page_rules.py --apply
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"

HEADING = re.compile(r"^#{1,6}\s")
# A rule directly under a PARAGRAPH makes it an H2. Under an HTML block (`</pre>`) or a blockquote it is a
# plain thematic break, because those close before it.
PARAGRAPH_ABOVE = re.compile(r"^(?![>#\-*|<`\s]).+")


def body_start(lines):
    """The first line of the standard proper — everything above it is hand-authored front matter."""
    for i, l in enumerate(lines):
        if l.strip() == '<a id="section-1"></a>':
            return i
    sys.exit("FATAL: the clause-1 anchor is missing; cannot tell the front matter from the body")


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
    start = body_start(lines)
    stats = collections.Counter()
    drop = []
    for i in range(start, len(lines)):
        if lines[i].strip() != "---":
            continue
        if PARAGRAPH_ABOVE.match(lines[i - 1] or ""):
            stats["kept — would demote a setext heading"] += 1
            continue
        drop.append(i)
        stats["removed"] += 1
    stats["kept — front matter"] = sum(1 for i in range(start) if lines[i].strip() == "---")

    for k, v in stats.most_common():
        print(f"  {v:5}  {k}")

    before_words = collections.Counter("\n".join(lines).split())
    before_heads = sum(1 for l in lines if HEADING.match(l))
    kept = [l for i, l in enumerate(lines) if i not in set(drop)]
    after_words = collections.Counter("\n".join(kept).split())
    after_heads = sum(1 for l in kept if HEADING.match(l))

    lost = (before_words - after_words) - collections.Counter({"---": len(drop)})
    gained = after_words - before_words
    print(f"\nwords   : {sum(before_words.values())} → {sum(after_words.values())}")
    print(f"headings: {before_heads} → {after_heads}")
    if lost or gained or before_heads != after_heads:
        print(f"  ✗ lost {list(lost.elements())[:6]}  gained {list(gained.elements())[:6]}")
        sys.exit("FATAL: removing the rules changed the content — it may only remove `---` lines")
    print("  ✓ only `---` lines removed; every word and every heading survived")

    if args.apply:
        SPEC_MD.write_text("\n".join(kept) + "\n", encoding="utf-8", newline="\n")
        print(f"\napplied to {SPEC_MD.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
