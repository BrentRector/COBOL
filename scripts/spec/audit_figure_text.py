#!/usr/bin/env python3
"""Audit the WORDS of every printed general format against the transcription.

THE GAP THIS CLOSES. `audit_underlining.py` checks the notation (which words are required) and
`figure_geometry.py` checks the delimiters (bracket, brace, choice indicator). Neither checks the thing
underneath both: whether the transcription got the figure's WORDS right. That matters more here than anywhere
else in the document, because `specs/ISO_COBOL.md` was produced by sending each page IMAGE to a vision model —
every word in it is a transcription of a picture, and a general format is exactly the kind of dense, low-context
layout such a model is most likely to guess at.

WHAT IT COMPARES. For each PDF page, the tokens printed on FIGURE lines; against every token anywhere in that
page's markdown. Reporting "printed but nowhere on the markdown page" rather than "not inside a fence" is
deliberate — the transcription renders some general formats fenced and some inline with `<u>` tags, and the
question worth asking is whether the content SURVIVED, not which style carried it.

FIGURE LINES are separated from prose by their lower-case words: in a general format every lower-case term is
hyphenated (`identifier-1`, `rounded-phrase`, `imperative-statement-2`), while prose is full of plain words like
"the" and "statement". Running headers are dropped explicitly, and layout labels ("Format", "General") are
ignored because the transcription places them outside the figure.

HYPHENATION IS RECONCILED, NOT COUNTED. The PDF breaks long hyphenated terms across lines, so `B-SHIFT-LC` is
printed as `B-SHIFT` + `LC` and `method-name-1` as `method-` + `name-1`. A missing token that is a prefix or
suffix of a token present in the markdown is therefore reported separately as a line-break artifact rather than
as content loss. Without this the report is dominated by noise that looks exactly like real findings.

    python scripts/spec/audit_figure_text.py
    python scripts/spec/audit_figure_text.py --pages 600 700 --show
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

TOKEN = re.compile(r"[A-Za-z][A-Za-z0-9]*(?:-[A-Za-z0-9]+)*")
RUNNING_HEADER = re.compile(r"ISO/IEC\s*1989")
# Layout labels the transcription deliberately keeps outside the figure.
IGNORE = {"ISO", "IEC", "Format", "Formats", "General", "FORMAT", "FORMATS", "NOTE", "Table"}


def page_slices(md_lines):
    marks = []
    for i, l in enumerate(md_lines):
        m = re.match(r'^<a id="page-(\d+)"></a>', l.strip())
        if m:
            marks.append((int(m.group(1)), i))
    return _require_page_anchors({p: (i, marks[k + 1][1] if k + 1 < len(marks) else len(md_lines))
                                  for k, (p, i) in enumerate(marks)})


def figure_tokens(page, extract):
    """Tokens printed on this page's general-format lines."""
    by_line = collections.defaultdict(list)
    for w in extract(page):
        by_line[round(w["y0"] / 3)].append(w)
    out = collections.Counter()
    for row in by_line.values():
        text = " ".join(w["text"] for w in sorted(row, key=lambda w: w["x0"]))
        if RUNNING_HEADER.search(text):
            continue
        if any(re.fullmatch(r"[a-z]{2,}", w["text"].strip(" []{}.,;:()")) for w in row):
            continue                                   # a plain lower-case word: prose, not a general format
        for w in row:
            for t in TOKEN.findall(w["text"]):
                if len(t) > 1 and t not in IGNORE:
                    out[t] += 1
    return out


def hyphen_artifact(token: str, present: set[str]) -> bool:
    """True if this token is one half of a hyphenated term the PDF broke across a line."""
    for p in present:
        if p == token:
            continue
        if (p.startswith(token + "-") or p.endswith("-" + token)
                or token.startswith(p + "-") or token.endswith("-" + p)):
            return True
        # The break need not fall on a hyphen: "SMALLEST-ALGEBRAIC" is printed as "SMALLEST-ALGE-" + "BRAIC",
        # so the orphan is a bare suffix of the whole term rather than a hyphen-delimited component.
        if len(token) >= 4 and (p.endswith(token) or p.startswith(token)):
            return True
    return False


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
    ap.add_argument("--show", action="store_true", help="list the tokens for each flagged page")
    ap.add_argument("--json")
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

    findings, stats = [], collections.Counter()
    for pno in range(lo, min(hi, doc.page_count) + 1):
        if pno not in slices:
            continue
        printed = figure_tokens(doc[pno - 1], extract)
        if not printed:
            continue
        stats["pages_with_figures"] += 1
        stats["figure_tokens"] += sum(printed.values())

        # Compare against this page AND its neighbours. Content legitimately drifts across a page boundary in
        # the transcription: a general rule that spans a break is merged into the preceding page, and the
        # column-wrapped reserved-word lists attribute the last few entries to the previous page. Checking one
        # page in isolation reported "OBJECT, OBJECT-COMPUTER, OCCURS, OF are missing from page 237" when all
        # four sit two lines above its anchor. The question worth asking is whether content SURVIVED.
        s = slices[max(pno - 1, min(slices))][0]
        e = slices.get(pno + 1, slices[pno])[1]
        # Strip only REAL HTML tags, by name. Anything looser deletes spec content and invents losses, because
        # angle brackets are ordinary characters in this document. `<[^>]+>` swallowed whole table rows between
        # a `<=` and a later `>` (producing a false "PUSH and READ-PREVIOUS are missing"); restricting it to a
        # single line still ate `< left-part OR selection-subject >` on page 652 and the `<blank>` /
        # `<Control Heading lines>` placeholders in the report-layout figures on page 1162. Three separate false
        # findings out of one over-broad regex, so the tag names are spelled out.
        body = re.sub(r"</?(?:u|b|i|a|em|strong|sub|sup|br|code|span)\b[^>\n]*>", " ", "\n".join(md[s:e]))
        present = set(TOKEN.findall(body))

        lost, hyphen = [], []
        for t in (printed - collections.Counter(TOKEN.findall(body))):
            (hyphen if hyphen_artifact(t, present) else lost).append(t)
        stats["lost"] += len(lost)
        stats["hyphen"] += len(hyphen)
        if lost:
            findings.append({"page": pno, "lost": sorted(lost), "hyphen": sorted(hyphen)})

    print(f"pages carrying a printed general format : {stats['pages_with_figures']}")
    print(f"figure tokens compared                  : {stats['figure_tokens']}")
    print(f"line-break hyphenation artifacts        : {stats['hyphen']}  (reconciled, not defects)")
    print(f"\nPRINTED IN A FIGURE, ABSENT FROM THE MARKDOWN PAGE : {stats['lost']}"
          f"  on {len(findings)} page(s)")
    if args.show or len(findings) <= 40:
        for f in findings:
            print(f"  p{f['page']:<5} {', '.join(f['lost'])}")
    if args.json:
        pathlib.Path(args.json).write_text(json.dumps(findings, indent=2), encoding="utf-8", newline="\n")
        print(f"\nwrote {args.json}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
