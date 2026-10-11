#!/usr/bin/env python3
"""Turn the index's PAGE references into intra-document CLAUSE links.

WHY. A page number is a layout artifact of one typesetting of the standard; in Markdown it points at nothing a
reader can use. The transcription's index carries ~3,270 of them, and they are the last thing standing between
the document and the removal of its page scaffolding altogether.

WHAT A REFERENCE BECOMES, in order of precision:

  1. **The term IS a clause title.** `USAGE clause [503]` → the clause whose heading is "USAGE clause". Exact:
     the index term and the heading are the same words, so the target is not a guess.
  2. **Every word of the term appears on the referenced page.** The clause in effect at that line.
  3. **Otherwise, the clause in effect where the page begins.** Approximate — a page can span several clauses,
     so a reference can land at the top of the right page's clause rather than exactly at the term.

⚠ THE THIRD CASE IS LOSSY AND IS REPORTED AS SUCH. `--report` prints the split so the trade is visible rather
than buried; roughly half the references resolve exactly and half by the page-start fallback. Nothing is
silently dropped: a reference that cannot be resolved at all keeps its page number as plain text, so no index
entry ever loses a pointer.

    python scripts/spec/deindex_pages.py --report
    python scripts/spec/deindex_pages.py --apply
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"

PAGE_ANCHOR = re.compile(r'^<a id="page-(\d+)"></a>')
HEADING = re.compile(r"^#+ ([A-Z]?\.?\d+(?:\.\d+)*) (.+)$")
PAGE_REF = re.compile(r"\[(\d+)\]\(#page-(\d+)\)")


def slug(num: str) -> str:
    return "section-" + num.lower().replace(".", "-").strip("-")


def build_maps(lines):
    """(clause in effect per line, {clause title -> number}, {page -> line span})."""
    clause_at, titles, cur = [], {}, None
    for l in lines:
        m = HEADING.match(l)
        if m:
            cur = m.group(1)
            titles.setdefault(m.group(2).strip().lower(), m.group(1))
        clause_at.append(cur)
    marks = [(int(m.group(1)), i) for i, l in enumerate(lines) if (m := PAGE_ANCHOR.match(l.strip()))]
    pages = {p: (i, marks[n + 1][1] if n + 1 < len(marks) else len(lines)) for n, (p, i) in enumerate(marks)}
    return clause_at, titles, pages


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
    clause_at, titles, pages = build_maps(lines)
    try:
        idx = next(i for i, l in enumerate(lines) if l.strip() == "## Index")
    except StopIteration:
        sys.exit("FATAL: no '## Index' heading found")

    stat = collections.Counter()
    out = list(lines)
    for i in range(idx, len(lines)):
        line = lines[i]
        if "(#page-" not in line or line.startswith("## Page ["):
            continue
        term = re.sub(r"&nbsp;", "", line.split("[")[0]).strip()
        words = re.findall(r"[A-Za-z][A-Za-z\-]{2,}", term)
        exact = titles.get(term.lower())

        def resolve(m):
            pno = int(m.group(2))
            if exact:
                stat["exact: term is a clause title"] += 1
                return f"[{exact}](#{slug(exact)})"
            span = pages.get(pno)
            if span:
                a, b = span
                # A DEFINED TERM is set in bold on its own line under its own numbered clause — clause 3 is
                # built entirely that way. Matching the index term against those lines resolves exactly.
                for k in range(a, b):
                    if lines[k].strip().lower() == f"**{term.lower()}**" and clause_at[k]:
                        stat["exact: term is a defined term"] += 1
                        return f"[{clause_at[k]}](#{slug(clause_at[k])})"
                hit = next((k for k in range(a, b)
                            if words and all(w.lower() in lines[k].lower() for w in words)), None)
                if hit is not None and clause_at[hit]:
                    stat["exact: term found on the page"] += 1
                    return f"[{clause_at[hit]}](#{slug(clause_at[hit])})"
                # Prefer the first clause that STARTS on the page over the one merely running through its
                # top: an anchor sits at the page break, so the clause "in effect" there is the tail of the
                # PREVIOUS page. That is how "Literal continuation indicator" pointed at all of clause 3.
                nxt = next((clause_at[k] for k in range(a, b) if HEADING.match(lines[k])), None)
                target = nxt or clause_at[a]
                if target:
                    stat["approximate: first clause on the page" if nxt else
                         "approximate: clause running through the page top"] += 1
                    return f"[{target}](#{slug(target)})"
            stat["unresolved — page number kept as text"] += 1
            return m.group(1)

        new = PAGE_REF.sub(resolve, line)
        # One clause can absorb several page references; collapse the repeats an entry would otherwise show.
        new = re.sub(r"(\[([^\]]+)\]\(#[^)]+\))(?:,\s*\1)+", r"\1", new)
        out[i] = new

    total = sum(stat.values())
    print(f"index references processed: {total}")
    for k, v in stat.most_common():
        print(f"  {v:5d}  ({100*v/total:4.1f}%)  {k}")

    if args.apply:
        SPEC_MD.write_text("\n".join(out) + "\n", encoding="utf-8", newline="\n")
        print(f"\napplied to {SPEC_MD}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
