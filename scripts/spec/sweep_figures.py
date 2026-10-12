#!/usr/bin/env python3
"""Replace every general format in the transcription with one generated from the printed page.

KEYED ON THE CLAUSE HIERARCHY, NEVER ON THE PAGE. A page number is a layout artifact — figures straddle page
breaks, and one clause's formats routinely span two pages — so a sweep keyed on "the nth block of page N"
mis-targets, and a mis-targeted replacement still looks like a figure. A figure's identity is its CLAUSE and
its FORMAT number (14.9.8.2 Format 2), which is what the printed page and the transcription agree on.

THE GATE, which is what makes this safe: a replacement is applied ONLY where the generated figure carries the
SAME WORDS as the text it replaces. Notation may change freely — that is the entire point of the sweep — but a
word appearing, vanishing or changing means the target is wrong or the page is one of the transcription's known
defects, and the figure is reported instead of written. `--report` shows what would happen without touching
anything; `--check` regenerates against the file and is the post-sweep regression gate.

THE TRANSCRIPTION DOES NOT FENCE EVERY FIGURE. Roughly a third of the general formats are one-liners set as
bare paragraphs — `\\>\\>compiler-instruction` on folio 54, `inline-invocation-1` and `identifier-2 object-view-1`
on folio 125. They are figures too, and they are where defects hide: folio 730 runs the NEXT format's label
into the figure line (`SET { identifier-7 } … TO identifier-8 Format 10 (data-pointer-arithmetic):`). Sweeping
only the fenced ones would leave the worst-transcribed third behind, in the old style.

    python scripts/spec/sweep_figures.py --report
    python scripts/spec/sweep_figures.py --report --pages 600 660
    python scripts/spec/sweep_figures.py --apply
    python scripts/spec/sweep_figures.py --check
"""
from __future__ import annotations

import argparse
import collections
import io
import contextlib
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"
PDF = next(iter(sorted((REPO / "specs-private").glob("*COBOL*.pdf"))), None)

PAGE_ANCHOR = re.compile(r'^<a id="page-(\d+)"></a>\s*$')
MD_HEADING = re.compile(r"^\s*#{1,6}\s+\**(\d+(?:\.\d+)+)\s+(.+?)\**\s*$")
MD_FORMAT = re.compile(r"^\s*\**Formats?\s+(\d+)")
MD_GENERAL_FORMAT = re.compile(r"general\s+formats?\s*$", re.I)
ADDENDUM_REF = re.compile(r"see the Addendum \(C\d+\)")
# A fence may carry a LANGUAGE TAG (```cobol on the example programs). Not matching it would leave the
# example's lines loose, where the bare-run scanner could mistake a COBOL example for a figure.
# `<pre>` carries a `line-height:1` style attribute (see render_figure.PRE_OPEN — it is what makes the
# box-drawing strokes meet), so the opening tag must be matched with its attributes, not as a bare tag.
FENCE = re.compile(r"^\s*(?:>\s?)?(?:```[a-zA-Z]*|</?pre(?:\s[^>]*)?>)\s*$")
FORMAT_LABEL = re.compile(r"^\s*(?:>\s?)?\*{0,2}Formats?\s+\d+\b")
HEADING = re.compile(r"^\s*#{1,6}\s")
# A row of a figure that carries only DELIMITERS — the blank row inside a group, drawn with the group's own
# stems. It is figure content, but it can neither open nor close a run: treating it as prose chopped every
# generated figure into as many spans as it had such rows.
NOTATION_ONLY = re.compile(r"^[\s│┤├╭╮╰╯┌┐└┘─|]*$")
ART_ONLY = re.compile(r"^[\sL⌐¬|┌┐└┘─│]*$")     # an ASCII-art corner row: `⌐   ¬` above, `L   ┘` below
# A blockquote, but NEVER a compiler directive: `>>POP` and `>> SOURCE FORMAT` are figure CONTENT. Matching
# them as quotes made the sweep re-emit a figure's own first line after the figure, as though it were a note.
BLOCKQUOTE = re.compile(r"^\s*>(?!>)")
HTML_LINE = re.compile(r"^\s*<")
HRULE = re.compile(r"^\s*-{3,}\s*$")           # a Markdown horizontal rule: page furniture by another name
# PAGE FURNITURE inside the transcription: the anchors, `## Page N` headings, running headers and folios the
# OCR carried across. It is not content, it is not a separator, and it is on its way OUT of the document
# (pages are not a thing in Markdown). Until then it must be TRANSPARENT: `**ISO/IEC 1989:2023 (E)**` passes
# the figure test — all upper case, no plain lower-case word — so it was being swept INTO figures, and a page
# break falling inside a figure split that figure into two spans that matched nothing.
PAGE_FURNITURE = re.compile(
    r"""^\s*(?:<a\ id="page-\d+"></a>
        |\#*\s*Page\ \d+
        |\#*\s*\**ISO/IEC\s+1989:2023\s*\(E\)\**
        |\**©\s*ISO/IEC\s+\d{4}\**
        |Licensed\ to\ .*
        |\d{1,4})\s*$""", re.X)

# Notation is expected to change; WORDS are not. Everything here is stripped before the comparison: the house
# box-drawing set, the Miscellaneous Technical extensions the transcription used until now, ASCII delimiters,
# and the two ellipsis spellings.
NOTATION = (set(chr(c) for c in range(0x2500, 0x2580)) | set(chr(c) for c in range(0x239B, 0x23AE))
            # `/` separates stacked alternatives when the transcription sets a group inline; `⌐ ¬ L` are
            # corners in the older ASCII-art figures. All three are notation, none is a word.
            | set("[]{}()|…/⌐¬"))


def unescape(line: str) -> str:
    """Markdown escaping the transcription applies to figure text (`\\>\\>SOURCE`, `\\*`, `\\_`)."""
    return re.sub(r"\\([>*_`\[\]])", r"\1", line)


def words_of(text: str, drop_notes: bool = False) -> collections.Counter:
    """The WORDS of a figure, with everything that is not a word removed.

    Tags, fence markers and back-ticks are markup, not content, and counting them made every comparison fail:
    the first run reported `<u>BY</u>` against `BY` and 229 "word differences" that were nothing of the kind.
    """
    # Strip a blockquote prefix, but never the first `>` of a COMPILER DIRECTIVE: `>>SOURCE FORMAT` is figure
    # text, and eating one of its angle brackets turned every directive format into a word mismatch.
    src = text.splitlines()
    if drop_notes:
        # ... but only OUTSIDE a fence. A good deal of the transcription sets its figures INSIDE a blockquote,
        # so every content line begins with `>` too; dropping those discarded the figures themselves.
        inside, filtered = False, []
        for l in src:
            if FENCE.match(l):
                inside = not inside
                filtered.append(l)
                continue
            if inside or not BLOCKQUOTE.match(l):
                filtered.append(l)
        src = filtered
    # PAGE FURNITURE carries no words either. `targets` already steps over it, but the comparison did not, so
    # a figure that straddles a page break was measured as containing "## Page 156".
    kept = [re.sub(r"^\s*>(?!>)\s?", "", l) for l in src
            if not FENCE.match(l) and not PAGE_FURNITURE.match(l) and not HRULE.match(l)]
    # `<u>` is ZERO-WIDTH — underlining never introduces a word boundary. Replacing it with a space split
    # `>><u>CALL-CONVENTION</u>` into two words and reported a difference against a markdown form whose
    # rendered text is identical, which is the wrong direction for a gate: it hides a real change behind a
    # spurious one. Every other tag still becomes a space, since those do separate content.
    body = re.sub(r"</?u>", "", unescape("\n".join(kept)))
    body = re.sub(r"</?[A-Za-z][^>]*>", " ", body).replace("`", " ")
    body = re.sub(r"&[a-zA-Z]+;?", " ", body)          # HTML entities used for indentation
    # The standard sets a dash four ways in figures — hyphen, figure dash, en dash, minus sign — and the
    # transcription picked one per site. Which glyph is used is typography, not a difference in the language.
    body = body.translate(str.maketrans({"‒": "-", "–": "-", "−": "-", "—": "-"}))
    # An ASCII-ART CORNER ROW carries no words. The older figures draw a bracket with `⌐ ¬` on top and
    # `L ┘` beneath, so the foot `L` reads as a capital letter — the only place a bare `L` appears in a
    # figure. Dropping whole art-only lines removes it without making `L` un-wordable everywhere.
    body = "\n".join("" if ART_ONLY.match(l) else l for l in body.splitlines())
    plain = "".join(" " if ch in NOTATION else ch for ch in body.replace("*", " "))
    plain = plain.replace("...", " ")
    # The separator period is punctuation, not a word, and the two sides disagree about whether it is attached:
    # the printed page gives `FACTORY.` as one token where the transcription writes `<u>FACTORY</u>.`. Trailing
    # punctuation is split off and standalone punctuation dropped, so the comparison stays about WORDS.
    out = collections.Counter()
    for w in plain.split():
        w = w.strip(".,;:")
        if w:
            out[w] += 1
    return out


def figure_ish(line: str) -> bool:
    """Whether a bare markdown line could be a general format rather than prose.

    Same test the generator uses on the printed page: a figure's lower-case terms are all hyphenated
    (identifier-1, rounded-phrase), while prose is full of plain words.
    """
    # The blockquote test runs on the RAW line. Unescaping first turns the transcription's `\>\>SOURCE FORMAT`
    # into `>>SOURCE FORMAT`, which reads as a blockquote — so every compiler-directive format was discarded
    # as prose and its clause reported as having no figure at all.
    if not line.strip() or BLOCKQUOTE.match(line) or PAGE_FURNITURE.match(line):
        return False
    # Strip tags BEFORE the html test: parts of the transcription already carry their underlining, and
    # `<u>NULL</u>` or `<u>FACTORY</u>. [ … ]` starts with `<`, so those clauses were reported as having no
    # figure at all when in fact they were the ones already closest to the target form.
    t = re.sub(r"</?[A-Za-z][^>]*>", "", unescape(line)).strip()
    if not t or HEADING.match(t) or HTML_LINE.match(t) or FORMAT_LABEL.match(t):
        return False
    if t.startswith("|") or t.startswith("---"):
        return False                                    # table row or rule
    toks = [w.strip(" []{}.,;:()<>") for w in t.split()]
    plain = [w for w in toks if re.fullmatch(r"[a-z]{2,}", w)]
    # As on the printed side: a plain lower-case word usually means prose, but the standard writes a few
    # metavariables unhyphenated — `identifier-1( leftmost-position : [ length ] )` and
    # `qualified-data-name-1 [ ( subscript … ) ]` are figures. NOTATION with only a word or two of lower case
    # settles it; prose in these regions carries no brackets, braces or ellipsis at all.
    if plain and not (len(plain) <= 2 and re.search(r"[\[\]{}…]|\.\.\.", t)):
        return False
    return bool(re.search(r"[a-z]+-[a-z0-9]|[A-Z]{2,}|^>>", t))


def md_index(lines):
    """{(clause, format-number) -> (start, end)} — where each figure's content lives in the transcription.

    The same structure the generator reads off the printed page: a numbered `… General format(s)` heading opens
    a region, the next heading of any level closes it, and `Format N (…)` labels divide it.
    """
    heads = [(i, m.group(1), m.group(2)) for i, l in enumerate(lines) if (m := MD_HEADING.match(l))]
    out = {}
    for n, (i, clause, title) in enumerate(heads):
        if not MD_GENERAL_FORMAT.search(title):
            continue
        end = heads[n + 1][0] if n + 1 < len(heads) else len(lines)
        marks = [(j, int(m.group(1))) for j in range(i + 1, end) if (m := MD_FORMAT.match(lines[j]))]
        if not marks:
            out[(clause, None)] = (i + 1, end)
            continue
        for k, (j, num) in enumerate(marks):
            out[(clause, num)] = (j + 1, marks[k + 1][0] if k + 1 < len(marks) else end)
    return out


def clause_spans(lines):
    """{clause -> (start, end)} for every `… General format(s)` region, labels or not."""
    heads = [(i, m.group(1), m.group(2)) for i, l in enumerate(lines) if (m := MD_HEADING.match(l))]
    out = {}
    for n, (i, clause, title) in enumerate(heads):
        if MD_GENERAL_FORMAT.search(title):
            out[clause] = (i + 1, heads[n + 1][0] if n + 1 < len(heads) else len(lines))
    return out


def targets(lines, lo, hi):
    """Ordered figure spans inside one page slice, as (start, end_exclusive, kind).

    Both shapes are found: a fenced block, and a bare run of figure-ish lines. Anything else — prose, notes,
    headings, tables, the `Format N (…)` labels — is skipped.
    """
    out, i, in_fence, start = [], lo, False, None
    run = None
    while i < hi:
        line = lines[i]
        if FENCE.match(line):
            if in_fence:
                out.append((start, i + 1, "fence"))
                in_fence = False
            else:
                if run is not None:
                    out.append((run, i, "bare"))
                    run = None
                in_fence, start = True, i
            i += 1
            continue
        if not in_fence:
            if figure_ish(line):
                if run is None:
                    run = i
            elif run is not None and (not line.strip() or PAGE_FURNITURE.match(line)
                                      or NOTATION_ONLY.match(line)):
                pass                                     # blanks, furniture and delimiter-only rows keep it open
            elif run is not None:
                out.append((run, i, "bare"))
                run = None
        i += 1
    if run is not None:
        out.append((run, hi, "bare"))
    # A FENCED block is a figure by construction — its lines are code, and the transcription blockquotes some
    # of them, so the prose test would throw them away. Only a BARE run has to prove it is not prose.
    keep = [(a, b, k) for a, b, k in out
            if k == "fence" or any(figure_ish(lines[j]) for j in range(a, b))]

    # ONE PRINTED FIGURE IS SOMETIMES SET AS SEVERAL BLOCKS. UNSTRING is split into two fences and a bare
    # `[ END-UNSTRING ]`; the options paragraph sets `OPTIONS.` above the fence holding its clauses. Adjacent
    # spans are therefore merged — but only across blanks, page furniture and the figure-notes blockquotes.
    #
    # A `Format N (…)` LABEL between them is the thing that must NOT be crossed, and it is exactly what
    # separates the qualification clause's ten figures from one another. Both cases are separated by a figure
    # note; only one is separated by a label, and that is the whole difference.
    def only_furniture(a, b):
        return all(not lines[j].strip() or PAGE_FURNITURE.match(lines[j]) or HRULE.match(lines[j])
                   for j in range(a, b))

    merged = []
    for span in keep:
        if merged and only_furniture(merged[-1][1], span[0]):
            merged[-1] = (merged[-1][0], span[1], span[2])
            continue
        merged.append(span)
    return merged


def note_lines(lines, a, b):
    """The figure NOTES inside a span — blockquote paragraphs only, never a blockquoted figure.

    A good deal of the transcription sets its figures INSIDE a blockquote, so their content lines carry the
    same `>` prefix a note does. Collecting by prefix alone re-emitted an entire old figure after its
    replacement; the fence state is what separates the two.
    """
    out, inside = [], False
    for j in range(a, b):
        if FENCE.match(lines[j]):
            inside = not inside
            continue
        if not inside and BLOCKQUOTE.match(lines[j]):
            out.append(lines[j])
    return out


def regroup(lines, spans, figures):
    """Group CONTIGUOUS markdown spans so that each group matches one printed figure, word for word.

    One printed figure is sometimes set as several blocks — UNSTRING is split into two fences and a bare
    `[ END-UNSTRING ]` — and consecutive figures are sometimes separated by nothing but a figure note. No
    fixed merge rule separates those two cases: crossing figure notes fixed UNSTRING and broke the
    file-control entry, whose formats are also separated by notes alone.

    So the grouping is not guessed, it is SOLVED, using the word gate as the decision procedure: find the
    partition of the spans into as many contiguous groups as there are figures, each group's words equal to
    its figure's. Returns None when no such partition exists, or when more than one does — an ambiguous
    grouping is reported rather than picked.
    """
    n, m = len(figures), len(spans)
    if not n or m < n:
        return None
    want = [words_of(chr(10).join(b)) for _, _, b in figures]
    solutions = []

    def walk(i, j, acc):
        if len(solutions) > 1:
            return
        if i == n:
            if j == m:
                solutions.append(list(acc))
            return
        for k in range(j, m):
            # Measure the MERGED RANGE, not the sum of the spans in it. A group spans the gaps between its
            # blocks too, and those gaps hold the figure notes — summing spans accepted groupings that would
            # have swallowed a note into the figure, which is how the file-control entry slipped through.
            total = words_of(chr(10).join(lines[spans[j][0]:spans[k][1]]), drop_notes=True)
            if total == want[i]:
                acc.append((spans[j][0], spans[k][1], "group"))
                walk(i + 1, k + 1, acc)
                acc.pop()
            elif not (want[i] - total):
                break                                   # already past the figure's words
    walk(0, 0, [])
    return solutions[0] if len(solutions) == 1 else None


def generated(doc, extract, classify_page, R):
    """Every figure in the standard, in reading order, grouped by CLAUSE.

    Grouped by clause and not by (clause, Format): a Format label does not identify a figure uniquely. The
    qualification clause ends with two unnumbered `where … is:` definitions that inherit the last label, so
    keying on the label merged three distinct figures into one and the clause then looked short by two.
    Within a clause both sides run top to bottom, so the nth figure is the nth figure.
    """
    out, failed = collections.OrderedDict(), []
    for pno in range(1, doc.page_count + 1):
        page = doc[pno - 1]
        for band in R.find_figures(page, extract):
            clause, fmt = R.figure_key(doc, pno, band)
            try:
                with contextlib.redirect_stdout(io.StringIO()):
                    grid, marks = R.build(page, band[0], band[1], extract, classify_page)
            except SystemExit as exc:
                failed.append((clause, pno, str(exc)))
                continue
            if grid is None:
                continue
            out.setdefault(clause, []).append((pno, fmt, R.render(grid, marks, False)))
    return out, failed


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--report", action="store_true", help="show what would change; write nothing")
    ap.add_argument("--apply", action="store_true", help="rewrite the transcription in place")
    ap.add_argument("--check", action="store_true", help="regenerate and diff against the file (regression gate)")
    ap.add_argument("--clause", help="restrict to one clause number, e.g. 14.9.8.2")
    ap.add_argument("--limit", type=int, default=40, help="how many problems to list")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if PDF is None or not PDF.exists():
        sys.exit("FATAL: the ISO PDF was not found under specs-private/. It is licensed per-copy and lives in "
                 "a PRIVATE submodule: git submodule update --init specs-private")
    import fitz

    sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
    import render_figure as R
    from figure_extract import extract
    from figure_geometry import classify_page

    doc = fitz.open(PDF)
    lines = SPEC_MD.read_text(encoding="utf-8").splitlines()
    index = md_index(lines)
    clause_span = clause_spans(lines)
    figs, failed = generated(doc, extract, classify_page, R)

    stats = collections.Counter()
    edits, problems = [], []
    for clause, figures in figs.items():
        if args.clause and clause != args.clause:
            continue
        stats["figures"] += len(figures)
        if clause is None:
            problems.append((clause, figures[0][0], None, "no clause could be resolved"))
            stats["no_clause"] += len(figures)
            continue
        whole = clause_span.get(clause)
        if whole is None:
            problems.append((clause, figures[0][0], None, "no such clause region in the transcription"))
            stats["no_target"] += len(figures)
            continue
        spans = targets(lines, *whole)
        if len(spans) != len(figures):
            spans = regroup(lines, spans, figures)
        if spans is None or len(spans) != len(figures):
            found = len(targets(lines, *whole))
            problems.append((clause, figures[0][0], None,
                             f"{len(figures)} printed figure(s) but {found} in the transcription, "
                             f"and no grouping of them matches"))
            stats["count_mismatch"] += len(figures)
            continue
        for (a, b, _kind), (pno, fmt, body) in zip(spans, figures):
            have = words_of(chr(10).join(lines[a:b]), drop_notes=True)
            want = words_of(chr(10).join(body))
            # A DOCUMENTED CORRECTION outranks the printed page. Where the transcription deliberately departs
            # from the standard it says so in place and lists the change in the Addendum, so regenerating from
            # the page would silently revert it — 12.3.6.2 corrects the standard's `locae-name-1` under C3.
            near = chr(10).join(lines[max(a - 3, 0):min(b + 8, len(lines))])
            if have != want and ADDENDUM_REF.search(near):
                stats["documented_correction"] += 1
                continue
            if have != want:
                problems.append((clause, pno, fmt, f"words differ — generated-only "
                                 f"{list((want - have).elements())[:4]}, markdown-only "
                                 f"{list((have - want).elements())[:4]}"))
                stats["word_mismatch"] += 1
                continue
            # Any figure NOTES that sat between the fragments are carried through, after the figure they
            # describe. They are the transcription's own analysis of the printed page and must not be lost.
            notes = note_lines(lines, a, b)
            repl = [R.PRE_OPEN] + body + ["</pre>"]
            if notes:
                repl += [""] + notes
            if lines[a:b] == repl:
                stats["already_current"] += 1
                continue
            edits.append((a, b, repl))
            stats["replaceable"] += 1

    print(f"figures generated                : {stats['figures']}")
    print(f"  replaceable (words match)      : {stats['replaceable']}")
    print(f"  already in generated form      : {stats['already_current']}")
    print(f"  no clause resolved             : {stats['no_clause']}")
    print(f"  no such clause region          : {stats['no_target']}")
    print(f"  figure-count mismatch          : {stats['count_mismatch']}")
    print(f"  documented correction (kept)   : {stats['documented_correction']}")
    print(f"  words differ                   : {stats['word_mismatch']}")
    print(f"  generator failed               : {len(failed)}")

    if (args.report or args.check) and problems:
        print(f"\nneeding attention ({len(problems)}):")
        for clause, pno, fmt, why in problems[:args.limit]:
            lbl = f" Format {fmt}" if fmt else ""
            print(f"  §{clause}{lbl} (folio {pno - 30})  {why}")
        if len(problems) > args.limit:
            print(f"  … and {len(problems) - args.limit} more")

    if args.apply:
        if not edits:
            print("\nnothing to apply")
            return 0
        out = list(lines)
        for a, b, repl in sorted(edits, reverse=True):
            out[a:b] = repl
        SPEC_MD.write_text("\n".join(out) + "\n", encoding="utf-8", newline="\n")
        print(f"\napplied {len(edits)} figure(s) to {SPEC_MD}")
        return 0
    if args.check:
        ok = not stats["replaceable"]
        print("\nCHECK: clean — every located figure matches its generated form." if ok else
              f"\nCHECK: {stats['replaceable']} figure(s) have DRIFTED from the generated form.")
        return 0 if ok else 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
