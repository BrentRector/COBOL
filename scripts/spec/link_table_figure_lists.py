#!/usr/bin/env python3
"""Anchor every table and figure caption, and rebuild the front-matter lists to LINK to them.

TWO DEFECTS, one cause — the lists were left keyed to the printed page.

  TABLES   The list carries a `Page` column of printed folios. Pages were removed from this transcription
           because they mean nothing in Markdown; a folio in a list of contents is exactly the reference a
           reader cannot follow.

  FIGURES  Worse than unhelpful: the list was WRONG. Its entries link `D.11` to `#section-d-11`, which is
           CLAUSE D.11 "Character sets" — not Figure D.11, "The VARYING phrase of a PERFORM statement with
           the TEST BEFORE phrase having one condition". Figure numbering and clause numbering are separate
           sequences that happen to share the annex letter, and whatever produced these links matched on the
           number alone. Twelve of the fifteen entries pointed at the wrong place; the other three were not
           links at all, and two printed folios sat orphaned on their own lines.

           The list also had no list markup, so its fifteen entries rendered as one run-on paragraph.

THE FIX is to anchor the captions themselves. Every table and figure in the body carries a bold caption —
`**Table 10 — …**`, `**Figure D.11 — …**` — so each gets an `<a id="table-10">` / `<a id="figure-d-11">`
immediately above it, and the lists are rebuilt from those captions. The list can then no longer disagree with
the document, because it is GENERATED FROM it rather than maintained beside it.

    python scripts/spec/link_table_figure_lists.py --report
    python scripts/spec/link_table_figure_lists.py --apply
"""
from __future__ import annotations

import argparse
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"

CAPTION = re.compile(r"^\*\*(Table|Figure) ([A-Z]?\.?[\d.]+) — (.+?)\*\*\s*$")


def slug(kind, num):
    return f"{kind.lower()}-{num.lower().replace('.', '-').strip('-')}"


def captions(lines):
    """Every table/figure caption in the BODY, in document order. The front-matter lists are excluded: they
    are what is being generated, and a caption there would make the output self-referential."""
    out = []
    body = next(i for i, l in enumerate(lines) if l.startswith("<a id=\"foreword\">"))
    for i in range(body, len(lines)):
        m = CAPTION.match(lines[i])
        if m:
            out.append((i, m.group(1), m.group(2), m.group(3)))
    return out


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
    found = captions(lines)
    tables = [(n, t) for _, k, n, t in found if k == "Table"]
    figures = [(n, t) for _, k, n, t in found if k == "Figure"]
    print(f"captions in the body : {len(found)}  ({len(tables)} tables, {len(figures)} figures)")

    dup = {n for n, _ in tables if [x for x, _ in tables].count(n) > 1}
    dup |= {n for n, _ in figures if [x for x, _ in figures].count(n) > 1}
    if dup:
        sys.exit(f"FATAL: duplicate caption numbers {sorted(dup)} — each must appear once, or the anchors "
                 f"collide and the lists point at whichever came last")

    # 1 — anchor each caption, from the bottom up so the earlier indices stay valid.
    added = 0
    for i, kind, num, _ in reversed(found):
        anchor = f'<a id="{slug(kind, num)}"></a>'
        if i and lines[i - 1].strip() == anchor:
            continue
        lines.insert(i, anchor)
        added += 1
    print(f"anchors added        : {added}")

    # 2 — rebuild the Tables list, keeping its two remaining columns and dropping the folios.
    ts = lines.index("## Tables")
    te = next(i for i in range(ts + 1, len(lines)) if lines[i].startswith("<a id=\"figures\">"))
    tbl = ["", "| # | Title |", "|---|---|"]
    tbl += [f"| {n} | [{t}](#{slug('Table', n)}) |" for n, t in tables]
    tbl += [""]

    # 3 — rebuild the Figures list as an actual list.
    fs = lines.index("## Figures")
    fe = next(i for i in range(fs + 1, len(lines)) if lines[i].startswith("<a id=\"foreword\">"))
    fig = [""] + [f"- [{n} {t}](#{slug('Figure', n)})" for n, t in figures] + [""]

    print(f"tables listed        : {len(tables)}   (was a table with a printed Page column)")
    print(f"figures listed       : {len(figures)}   (was {fe - fs - 1} unbulleted lines, 12 links wrong)")

    if args.apply:
        lines[fs + 1:fe] = fig                      # figures first: it is later in the file
        lines[ts + 1:te] = tbl
        SPEC_MD.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
        print(f"\napplied to {SPEC_MD.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
