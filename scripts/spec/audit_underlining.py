#!/usr/bin/env python3
"""Audit the transcription's UNDERLINING against the printed page, for every general format in the standard.

WHAT IS AT STAKE. §5.2.2 makes an underlined uppercase word REQUIRED; §5.2.3 makes a non-underlined one an
OPTIONAL word that may be written or omitted. Underlining is therefore load-bearing grammar carried entirely by
typography — and it is the detail a transcription loses silently, because losing it changes nothing structural.
Every gate stays green. The repairs so far have repeatedly found words *described* as underlined that are not
(falsely restrictive: a legal omission becomes a syntax error) and words underlined on the page that the
transcription never marks (falsely permissive: a required word looks droppable).

WHY IT CAN BE AUDITED NOW. The PDF's text layer decodes (`pdf_deobfuscate.py`) and underlines are long thin
horizontal rectangles in the vector data, so a word's underline state is a MEASUREMENT — see `figure_extract.py`.

THE SIGNAL IS CLEAN. Underlining is used in this document only for required words in general formats: a prose
page yields zero underlined reserved-word tokens, while page 613 yields exactly the required words of the three
ADD formats. So a mismatch is a real finding rather than noise.

TWO CHECKS, both precise, both able to fail:

  UNMARKED   the page underlines a word, and the markdown page neither wraps it in <u></u> nor mentions it in a
             figure note. The transcription gives no indication the word is required.  -> falsely PERMISSIVE
  OVERMARKED the markdown wraps a word in <u></u>, but no occurrence of that word is underlined on the page.
             The transcription invents a requirement.                                  -> falsely RESTRICTIVE

  MIXED      reported separately, and NOT a defect by itself: the page underlines SOME occurrences of a word and
             not others. `EXCEPTION` on page 653 is the real example — underlined in `EXCEPTION exception-name-1`,
             bare in `LAST EXCEPTION`. Set-based comparison cannot express that, so these need eyes.

DELIBERATE LIMIT. A figure note that *names* a word is treated as "mentioned", not as agreeing about it — the
note's prose is not parsed. That makes UNMARKED conservative: it under-reports rather than crying wolf. Notes are
checked by reading them, which is how the ON/OFF family was repaired.

⚠ KNOWN FALSE-POSITIVE CLASS: TABLES. A table's horizontal ROW BORDER is a long thin rule sitting just under the
row's text, which is geometrically indistinguishable from an underline. On table pages every cell in a bordered
row therefore reads as "underlined" — page 628 shows it plainly, where the footnote markers `c`, `c,f`, `a,c,f`
all come back underlined. Most MIXED results on table-heavy pages (564, 628, and the intrinsic-function summary
pages in the 860-970 range) are this artifact, not a defect. MIXED is a QUEUE FOR INSPECTION, never a finding.

⚠ AND THE BUG THIS TOOL ALREADY MADE, kept here as a warning. Its first run reported two OVERMARKED defects,
`IN` on page 322 and `TO` on page 673 — both wrong. `figure_extract.py` was excluding horizontal rules under
9 pt as bracket feet, and the underline under a two-letter word is 8.4-8.9 pt. The tool was accusing a CORRECT
transcription, and acting on it would have stripped real underlining out of the reference document. Width cannot
separate a foot from an underline; position can. Whenever this tool reports a defect, confirm it against the raw
rectangles before changing anything.

    python scripts/spec/audit_underlining.py                 # whole standard
    python scripts/spec/audit_underlining.py --pages 600 700
    python scripts/spec/audit_underlining.py --json out.json
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
PDF = next(iter(sorted((REPO / "specs-private").glob("*COBOL*.pdf"))), None)

# A COBOL reserved word as printed in a general format: uppercase, digits and hyphens.
RESERVED = re.compile(r"^[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*$")
UTAG = re.compile(r"<u>\s*([^<]+?)\s*</u>", re.I)
# Words the standard prints underlined that are ordinary English elsewhere; ignoring them avoids noise from
# prose that merely happens to contain the token.
MIN_LEN = 2


def page_slices(md_lines):
    """page number -> (start, end) line indices of that page's markdown."""
    marks = []
    for i, l in enumerate(md_lines):
        m = re.match(r'^<a id="page-(\d+)"></a>', l.strip())
        if m:
            marks.append((int(m.group(1)), i))
    out = {}
    for k, (pno, i) in enumerate(marks):
        out[pno] = (i, marks[k + 1][1] if k + 1 < len(marks) else len(md_lines))
    return _require_page_anchors(out)


# ⛔ PAGES WERE REMOVED FROM THE TRANSCRIPTION (2026-07-27). This audit buckets the Markdown by page anchor to
# compare it against the corresponding PDF page, and with the anchors gone it finds nothing — which it would
# otherwise report as a clean run. A silent all-clear is the one failure this directory exists to prevent, so
# it stops instead. Figures are now GENERATED from the printed page and `sweep_figures.py --check` proves it
# exactly, which supersedes this audit for anything figure-shaped; re-keying it onto the clause structure is
# what it needs to live on for the rest.
def _require_page_anchors(figs):
    if not figs:
        sys.exit("HALTED: no page anchors in specs/ISO_COBOL.md — pages were removed from the transcription, "
                 "so this page-keyed audit can no longer bucket it. For figures use "
                 "`python scripts/spec/sweep_figures.py --check`, which regenerates and diffs exactly. "
                 "This audit needs re-keying onto the clause hierarchy.")
    return figs


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--pages", nargs=2, type=int, metavar=("FROM", "TO"))
    ap.add_argument("--json")
    ap.add_argument("--quiet", action="store_true", help="totals only")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if PDF is None or not PDF.exists():
        sys.exit("FATAL: the ISO PDF was not found under specs-private/. It is licensed per-copy and lives in a PRIVATE submodule; the public repository carries only the Markdown transcription at specs/ISO_COBOL.md. This tool measures the printed page, so it needs the PDF: "
             "git submodule update --init specs-private")
    import fitz

    sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
    from figure_extract import extract

    doc = fitz.open(PDF)
    md = SPEC_MD.read_text(encoding="utf-8").splitlines()
    slices = page_slices(md)
    lo, hi = args.pages if args.pages else (1, doc.page_count)

    findings = []
    stats = collections.Counter()
    for pno in range(lo, min(hi, doc.page_count) + 1):
        if pno not in slices:
            continue
        # Restrict to FIGURE lines. A reserved word also appears in the prose of the syntax and general rules,
        # where it is never underlined — counting those made almost every statement look "mixed" (ADD underlined
        # in its format, plain in its own description). The discriminator is lower-case words: in a general
        # format every lower-case term is hyphenated (identifier-1, rounded-phrase, imperative-statement-2),
        # whereas prose is full of plain words like "the" and "statement".
        by_line = collections.defaultdict(list)
        for w in extract(doc[pno - 1]):
            by_line[round(w["y0"] / 3)].append(w)
        words = []
        for row in by_line.values():
            if any(re.fullmatch(r"[a-z]{2,}", w["text"].strip(" []{}.,;:()")) for w in row):
                continue                      # a plain lower-case word: prose, not a general format
            words.extend(row)

        under = collections.Counter()
        total = collections.Counter()
        for w in words:
            t = w["text"].strip(" []{}.,;:()")
            if len(t) < MIN_LEN or not RESERVED.match(t):
                continue
            total[t] += 1
            if w["underlined"]:
                under[t] += 1
        if not total:
            continue
        stats["pages_with_formats"] += 1

        s, e = slices[pno]
        body = "\n".join(md[s:e])
        tagged = {m.strip() for m in UTAG.findall(body)}
        # "mentioned" = appears anywhere in the page's markdown at all
        present = {t for t in total if re.search(r"\b" + re.escape(t) + r"\b", body)}

        unmarked = sorted(t for t in under if t not in tagged and t not in present)
        # a word may be tagged on the page but genuinely underlined nowhere on it
        overmarked = sorted(t for t in tagged if RESERVED.match(t) and t in total and under.get(t, 0) == 0)
        mixed = sorted(t for t in under if 0 < under[t] < total[t])

        stats["underlined_tokens"] += sum(under.values())
        if unmarked or overmarked or mixed:
            findings.append({"page": pno, "unmarked": unmarked, "overmarked": overmarked, "mixed": mixed})
            stats["unmarked"] += len(unmarked)
            stats["overmarked"] += len(overmarked)
            stats["mixed"] += len(mixed)

    print(f"pages carrying general-format underlining : {stats['pages_with_formats']}")
    print(f"underlined reserved-word tokens measured  : {stats['underlined_tokens']}")
    print(f"\nUNMARKED   (underlined on page, absent from the markdown page) : {stats['unmarked']}")
    print(f"OVERMARKED (<u> in markdown, not underlined on the page)       : {stats['overmarked']}")
    print(f"MIXED      (some occurrences underlined, some not — needs eyes): {stats['mixed']}")

    if not args.quiet:
        for kind in ("overmarked", "unmarked", "mixed"):
            rows = [f for f in findings if f[kind]]
            if not rows:
                continue
            print(f"\n=== {kind.upper()} — {sum(len(f[kind]) for f in rows)} on {len(rows)} pages ===")
            for f in rows[:60]:
                print(f"  p{f['page']:<5} {', '.join(f[kind])}")
            if len(rows) > 60:
                print(f"  … and {len(rows) - 60} more pages")

    if args.json:
        pathlib.Path(args.json).write_text(json.dumps(findings, indent=2), encoding="utf-8", newline="\n")
        print(f"\nwrote {args.json}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
