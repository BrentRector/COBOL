#!/usr/bin/env python3
"""Audit the DELIMITER STRUCTURE of every general format: printed page versus transcription.

THE GAP THIS CLOSES, and it is the last one. Three whole-standard sweeps already exist:

    audit_figure_text.py    the WORDS of each figure          1 discrepancy in 15,625 tokens
    audit_underlining.py    which words are REQUIRED (5.2.2)  0 defects in 2,215 tokens
    figure_geometry.py      whether a group carries BARS      exception-phrase class closed 30/30

None of them checks the thing a reader of a general format depends on most: **which delimiter encloses which
rows**. A bracket that should span four rows but is drawn around one, a brace transcribed as a bracket, an outer
bracket dropped entirely — every one of those changes what the language permits, and every one survives all three
existing audits untouched. The reconciliation found real examples (lost outer brackets; garbled nesting in SORT
Format 2; a brace rendered as a bracket in MERGE), which is what makes this worth measuring rather than assuming.

HOW THE PRINTED STRUCTURE IS RECOVERED. Two different mechanisms, because the standard typesets the two families
differently — a fact established the hard way earlier in this work:

  * **brackets and choice-indicator bars are VECTOR RECTANGLES.** A bracket stem carries short horizontal feet; a
    bar is a bare rule. `figure_geometry.classify_page` separates them.
  * **braces are FONT GLYPHS** from the Symbol face — extensible pieces assembled into a tall brace. The top
    piece appears exactly once per brace, so counting left-brace tops counts brace pairs.

HOW THE TRANSCRIBED STRUCTURE IS RECOVERED. The house style draws tall delimiters with the Unicode extension
glyphs (U+23A1…U+23AD), one top piece per delimiter — so the same counting works on the Markdown.

⚠ WHAT A MISMATCH DOES AND DOES NOT MEAN — read this before quoting the number. It compares COUNTS PER PAGE,
not geometry, so its output is a SHORTLIST and not a defect list:

  * a page whose counts AGREE can still nest wrongly — counts are blind to which delimiter encloses what;
  * a page whose counts DIFFER is more often a rendering-convention difference than a defect. Page 393 draws
    `{ BIT } / { NATIONAL }` by repeating the brace on consecutive rows instead of using extension glyphs, and
    was scored as five missing braces when nothing at all is missing.

Both conventions are counted now, which helps without settling it: over the whole standard roughly 46 of 161
general-format pages disagree, and that is an UPPER BOUND still contaminated by layout. It is NOT a count of
broken figures and must not be quoted as one.

Making it precise means comparing per-figure GEOMETRY — which delimiter spans which rows, and what nests inside
what — on both sides, rather than totals per page. That is a larger piece of work and is not what this file does.

Confirm every candidate against the printed page before editing. Every audit in this directory has at some point
accused a correct page, and this is the least precise of them.

AMBIGUOUS RENDERINGS are reported separately. Where a figure draws a tall delimiter by repeating `[` on
consecutive rows rather than using the extension glyphs, its structure is genuinely unrecoverable from the text:
`[ [ AT END … ] ]` reads equally as a nested bracket or as one tall bracket carrying bars. That ambiguity is not
hypothetical — it is exactly how the READ statement's choice indicators were lost.

    python scripts/spec/audit_figure_structure.py
    python scripts/spec/audit_figure_structure.py --pages 600 830 --show
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

# Symbol-face extensible brace pieces, as they decode once the text layer is readable.
BRACE_LEFT_TOP, BRACE_RIGHT_TOP = "ì", "ü"
# House-style Unicode extension glyphs used by the transcription.
MD_BRACKET_TOP, MD_BRACE_TOP = "⎡", "⎧"
MD_BRACKET_RIGHT_TOP, MD_BRACE_RIGHT_TOP = "⎤", "⎫"
MD_TALL = set("⎡⎢⎣⎤⎥⎦⎧⎨⎩⎫⎬⎭")
# The HOUSE style (FIGURE-STYLE.md) draws the same delimiters in box drawing: one `┌` per bracket, one `╭` per
# brace or parenthesis, and a brace is the one that also carries a point `┤`.
HOUSE_BRACKET_TOP, HOUSE_ROUND_TOP, HOUSE_POINT = "┌", "╭", "┤"


def md_figures(md_lines):
    """page number -> the figure lines on that page, from BOTH block forms.

    Generated figures are `<pre>` blocks, because a fenced block renders `<u>` literally and so cannot carry
    the underlining that 5.2.2 makes load-bearing. Reading only fences would report a swept document as having
    no figures at all — a silent all-clear, which is the failure mode this whole directory exists to remove.

    ⚠ Once a figure is GENERATED, `sweep_figures.py --check` supersedes this audit: it regenerates every figure
    and diffs, which is exact, where this compares counts per page and is only ever a shortlist.
    """
    page, fence, pre, out = None, False, False, collections.defaultdict(list)
    for l in md_lines:
        m = re.match(r'^<a id="page-(\d+)"></a>', l.strip())
        if m:
            page = int(m.group(1))
            continue
        s = l.strip()
        if s.startswith("```"):
            fence = not fence
            continue
        if s == "<pre>":
            pre = True
            continue
        if s == "</pre>":
            pre = False
            continue
        if (fence or pre) and page:
            out[page].append(re.sub(r"</?u>", "", l))
    return out


def md_structure(lines):
    """Tall-delimiter and bar counts as the transcription draws them."""
    text = "\n".join(lines)
    # Count bar COLUMNS, not '|' characters. On the printed page a choice indicator is ONE continuous vertical
    # rule spanning every row of its group; the transcription redraws it as a '|' on each row, so a two-row group
    # with a bar each side is 2 bars printed but 4 characters written. Counting characters reported every
    # correctly-transcribed group as a mismatch — this audit's own first bug, and the same shape as the others
    # this week: a measure that looks equivalent to the thing being measured, and is not.
    cols = collections.Counter()
    for l in lines:
        for i, ch in enumerate(l):
            if ch == "|":
                cols[i] += 1
    bars = len(cols)
    # A tall delimiter is written EITHER with the extension glyphs (one top piece each) OR in the older style,
    # by repeating '[' or '{' at the same column on consecutive rows. Counting only the glyphs scored every
    # figure in the older style as a mismatch — page 393 draws `{ BIT } / { NATIONAL }` that way and was
    # reported as five missing braces when nothing is missing. Both conventions are counted here; whether the
    # older one should SURVIVE is a separate question, and it is what `ambiguous_rows` measures.
    def repeated_runs(ch: str) -> int:
        runs, prev_cols, active = 0, set(), set()
        for l in lines:
            cols = {i for i, c in enumerate(l) if c == ch}
            for c in cols & prev_cols:
                if c not in active:
                    runs += 1
                    active.add(c)
            active &= cols
            prev_cols = cols
        return runs

    # A bar column is all `│`; a bracket column carries a corner. Counting `│` characters would score every
    # bracket's own shaft as a choice indicator.
    house_bars = sum(1 for i, n in cols.items()
                     if all(l[i] in " │" for l in lines if i < len(l)) and n > 1)
    return {
        "brackets": text.count(MD_BRACKET_TOP) + repeated_runs("[") + text.count(HOUSE_BRACKET_TOP),
        "braces": text.count(MD_BRACE_TOP) + repeated_runs("{") + text.count(HOUSE_POINT),
        "bars": bars if HOUSE_ROUND_TOP not in text and HOUSE_BRACKET_TOP not in text else house_bars,
        "tall_glyphs": sum(1 for l in lines for ch in l if ch in MD_TALL),
    }


def repeated_bracket_rows(lines) -> int:
    """Rows where a tall delimiter is drawn by repeating '[' or '{' instead of the extension glyphs.

    Two or more consecutive rows opening with the same delimiter at the same column is the older convention.
    Its structure cannot be read back out of the text, which is how READ's choice indicators were lost.
    """
    n, prev = 0, None
    for l in lines:
        m = re.match(r"^(\s*)([\[{])", l)
        key = (m.group(1), m.group(2)) if m else None
        if key and key == prev:
            n += 1
        prev = key
    return n


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
    ap.add_argument("--show", action="store_true", help="list every compared page, not only mismatches")
    ap.add_argument("--json")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if PDF is None or not PDF.exists():
        sys.exit("FATAL: the ISO PDF was not found under specs-private/. It is licensed per-copy and lives in a "
                 "PRIVATE submodule; the public repository carries only the Markdown transcription. This tool "
                 "measures the printed page: git submodule update --init specs-private")
    import fitz

    sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
    from figure_geometry import classify_page

    from figure_extract import extract

    def format_bands(page):
        """The y-bands occupied by GENERAL-FORMAT rows, so table rules are not counted as delimiters.

        A table's vertical grid line is geometrically identical to a choice-indicator bar, and a table page can
        carry dozens. Counting them reported 122 of 260 pages as disagreeing when the great majority were tables
        and the Figure-1 ruler. Rows are separated the same way the other audits separate them: in a general
        format every lower-case term is hyphenated (identifier-1, rounded-phrase), while prose and table cells
        are full of plain words.
        """
        rows = collections.defaultdict(list)
        for w in extract(page):
            rows[round(w["y0"] / 3)].append(w)
        bands = []
        for r in rows.values():
            text = " ".join(x["text"] for x in r)
            if re.search(r"ISO/IEC\s*1989", text):
                continue
            if any(re.fullmatch(r"[a-z]{2,}", x["text"].strip(" []{}.,;:()")) for x in r):
                continue
            ys = [x["y0"] for x in r]
            bands.append((min(ys) - 14, max(ys) + 14))
        return bands

    def in_band(y0, y1, bands):
        return any(not (y1 < b0 or y0 > b1) for b0, b1 in bands)

    doc = fitz.open(PDF)
    figs = _require_page_anchors(md_figures(SPEC_MD.read_text(encoding="utf-8").splitlines()))
    lo, hi = args.pages if args.pages else (1, doc.page_count)

    rows, stats = [], collections.Counter()
    for pno in range(lo, min(hi, doc.page_count) + 1):
        page = doc[pno - 1]
        # Only audit pages the STANDARD ITSELF labels as carrying a general format. The lower-case-word test
        # that separates figures from prose cannot separate them from a reserved-word TABLE — those are
        # all-uppercase too, and their grid rules are geometrically identical to choice-indicator bars. That is
        # why an unfiltered run reported 113 of 251 pages disagreeing: it was auditing the word lists.
        if "General format" not in page.get_text():
            continue
        bands = format_bands(page)
        if not bands:
            continue                                   # nothing on this page is a general format
        stems = [s for s in classify_page(page) if in_band(s["y0"], s["y1"], bands)]
        braces = 0
        for x0, y0, x1, y1, ch, *_ in page.get_text("words"):
            if BRACE_LEFT_TOP in ch and in_band(y0, y1, bands):
                braces += ch.count(BRACE_LEFT_TOP)
        printed = {
            "brackets": sum(1 for s in stems if s["kind"] == "bracket") // 2,
            "braces": braces,
            "bars": sum(1 for s in stems if s["kind"] == "bar"),
        }
        if not any(printed.values()):
            continue                                   # no delimited figure printed on this page
        stats["pages_with_printed_figures"] += 1

        lines = figs.get(pno, [])
        got = md_structure(lines)
        amb = repeated_bracket_rows(lines)
        diff = {k: got[k] - printed[k] for k in ("brackets", "braces", "bars")}
        mismatch = any(diff.values())
        if mismatch:
            stats["mismatched_pages"] += 1
            for k, v in diff.items():
                if v:
                    stats[f"{k}_delta"] += abs(v)
        if amb:
            stats["pages_with_ambiguous_rendering"] += 1
        rows.append({"page": pno, "printed": printed, "markdown": {k: got[k] for k in printed},
                     "delta": diff, "ambiguous_rows": amb, "mismatch": mismatch})

    print(f"pages printing a delimited figure : {stats['pages_with_printed_figures']}")
    print(f"pages whose counts DISAGREE       : {stats['mismatched_pages']}")
    print(f"pages using the ambiguous style   : {stats['pages_with_ambiguous_rendering']}")
    print(f"  bracket-count delta (total)     : {stats['brackets_delta']}")
    print(f"  brace-count   delta (total)     : {stats['braces_delta']}")
    print(f"  bar-count     delta (total)     : {stats['bars_delta']}")

    show = [r for r in rows if (args.show or r["mismatch"])]
    if show:
        print(f"\n{'page':>5}  {'printed [ { |':>16}  {'markdown [ { |':>16}  delta        ambiguous")
        for r in show[:80]:
            p, m, dl = r["printed"], r["markdown"], r["delta"]
            d = " ".join(f"{k[0]}{v:+d}" for k, v in dl.items() if v) or "-"
            print(f"{r['page']:>5}  {p['brackets']:>5} {p['braces']:>4} {p['bars']:>4}  "
                  f"{m['brackets']:>5} {m['braces']:>4} {m['bars']:>4}  {d:<12} {r['ambiguous_rows'] or ''}")
        if len(show) > 80:
            print(f"  … and {len(show) - 80} more")
    if args.json:
        pathlib.Path(args.json).write_text(json.dumps(rows, indent=2), encoding="utf-8", newline="\n")
        print(f"\nwrote {args.json}")
    print("\n⚠ counts, not geometry — confirm every candidate against the printed page before editing")
    return 0


if __name__ == "__main__":
    sys.exit(main())
