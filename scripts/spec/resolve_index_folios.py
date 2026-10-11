#!/usr/bin/env python3
"""Resolve the index references that de-paging left as bare printed folios.

57 index entries still read `Alphabetic character 3` — a printed page number with nothing to follow. The
index's own note says such references were left as text, but "documented" is not "resolved": the reader still
cannot get there.

A folio maps to the clause THE PAGE OPENS WITH, which is the same page-level approximation the note already
describes for the references converted earlier. Two things make that mapping trustworthy, and both were got
wrong on the first attempt:

  THE FRONT MATTER IS SKIPPED. It contains the table of contents, which is thousands of lines of
  `heading … number`. Scanning it for folios harvests TOC entries instead of page numbers, and the bad values
  land first and stick. Scanning starts at the page where clause 1 begins.

  THE CLAUSE IS THE ONE IN EFFECT AT THE TOP OF THE PAGE, carried forward from earlier pages — not the last
  heading printed on it. Most pages carry no heading at all: folios 5, 6 and 16 are all deep inside clause 3,
  Terms and definitions, and a "last heading on this page" rule silently attributes them to whatever heading it
  last saw, which is how folio 5 came out as clause 7.

⚠ A WRONG LINK IS WORSE THAN A PLAIN NUMBER. It misdirects silently, which is exactly the defect that made the
list of figures point at the wrong clauses. Every folio this script cannot map confidently keeps its printed
number, and `--report` shows what it would do before anything is written.

    python scripts/spec/resolve_index_folios.py --report
    python scripts/spec/resolve_index_folios.py --apply
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"
PDF = next(iter(sorted((REPO / "specs-private").glob("*COBOL*.pdf"))), None)

HEADING = re.compile(r"^\s*(\d+(?:\.\d+)*)\s+[A-Z]")
FOLIO = re.compile(r"©ISO/IEC 2023\s+(\d{1,4})\s*$|^\s*(\d{1,4})\s+©ISO/IEC 2023", re.M)
# A designation like ISO 1989 or ISO 8601 is not a folio.
STANDARD_NUMBER = re.compile(r"ISO|IEC|Unicode|UAX")


def folio_to_clause(doc):
    """printed folio -> the clause in effect where that page begins."""
    start = None
    for pno in range(doc.page_count):
        if re.search(r"^\s*1\s+Scope\s*$", doc[pno].get_text(), re.M):
            start = pno
            break
    if start is None:
        sys.exit("FATAL: could not find where clause 1 begins, so the front matter cannot be skipped")

    out, current = {}, None
    for pno in range(start, doc.page_count):
        text = doc[pno].get_text()
        opening = current                       # the clause this page OPENS with, before its own headings
        first_here = None
        for line in text.splitlines():
            h = HEADING.match(line)
            if h:
                first_here = first_here or h.group(1)
                current = h.group(1)
        m = FOLIO.search(text)
        if not m:
            continue
        folio = int(m.group(1) or m.group(2))
        # A page that STARTS a clause is attributed to that clause, not to the one running off the previous
        # page. Printed page 3 carries the heading `3 Terms and definitions`, and every index term pointing
        # at it is a definition — attributing it to clause 2, Normative references, is technically "the
        # clause the page opens with" and useless. Pages with no heading of their own (5, 6 and 16 are all
        # deep inside clause 3) still carry forward, which is the case that rule exists for.
        clause = first_here or opening or current
        if clause and folio not in out:
            out[folio] = clause
    return start, out


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--report", action="store_true")
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if PDF is None or not PDF.exists():
        sys.exit("FATAL: the ISO PDF was not found under specs-private/: "
                 "git submodule update --init specs-private")
    import fitz

    doc = fitz.open(PDF)
    start, mapping = folio_to_clause(doc)
    print(f"clause 1 begins at PDF page {start + 1}; folios mapped: {len(mapping)}")
    for folio in (3, 5, 6, 16, 25, 29):
        print(f"   folio {folio:>4} -> clause {mapping.get(folio, '(unmapped)')}")

    lines = SPEC_MD.read_text(encoding="utf-8").splitlines()
    i = next(k for k, l in enumerate(lines) if l.startswith("## Index"))
    j = next(k for k, l in enumerate(lines) if l.startswith("## Corrections applied"))

    anchors = set(re.findall(r'<a id="([^"]+)"></a>', "\n".join(lines)))
    stats = collections.Counter()
    changes = []
    for k in range(i, j):
        line = lines[k]
        if STANDARD_NUMBER.search(line):
            continue
        body = re.sub(r"^\s*-\s+", "", line)
        if not body or body[0].isdigit():
            continue                            # the number IS the term: `01 entry`, `66 RENAMES …`

        def repl(m):
            folio = int(m.group(1))
            clause = mapping.get(folio)
            anchor = f"section-{clause.replace('.', '-')}" if clause else None
            if anchor is None or anchor not in anchors:
                stats["unmapped"] += 1
                return m.group(0)
            stats["resolved"] += 1
            return m.group(0).replace(m.group(1), f"[{folio}](#{anchor})")

        new = re.sub(r"(?<=[a-z\)])\s(\d{1,4})(?=\s*(?:,|$))", repl, line)
        if new != line:
            changes.append((k, line, new))
            lines[k] = new

    print(f"\nresolved : {stats['resolved']}")
    print(f"unmapped : {stats['unmapped']}   (folio keeps its printed number)")
    for _, old, new in changes[:10]:
        print(f"   {old.strip()[:60]}\n     -> {new.strip()[:70]}")

    if args.apply:
        SPEC_MD.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
        print(f"\napplied to {SPEC_MD.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
