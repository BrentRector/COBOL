#!/usr/bin/env python3
"""Convert the last eight LaTeX general-format diagrams to the house ASCII style, with MEASURED underlining.

WHY THESE EIGHT. The transcription renders 514 general formats as fenced ASCII figures and eight as LaTeX. That
inconsistency is not cosmetic: the ASCII figures carry `Figure notes` blocks recording which words are underlined
and what the delimiters mean, and the LaTeX ones do not — so the notation that §5.2.2/§5.2.3 and §5.2.6 attach
meaning to was being carried in a form nothing checks. ACCEPT Format 3 (page 607) was one of them, and it turned
out to have lost its choice indicators AND to have mis-stated underlining in two places.

EVERY UNDERLINE HERE WAS READ OFF THE PAGE, not carried over from the LaTeX. `figure_extract.py` matches
underline rectangles to individual words; `figure_geometry.py` reports the bracket stems. Two normative defects
fell out that no sweep agent had reported:

  * **page 606, ACCEPT Format 2** — `YYYYMMDD` and `YYYYDDD` are UNDERLINED on the printed page (measured at
    y=571.5 x=242.1 and y=588.8 x=235.7). The LaTeX left them plain, which per §5.2.3 would make them optional
    words that may be omitted — i.e. it implied `ACCEPT x FROM DATE [ ]` is meaningful.

  * **page 653, EXIT Format 2** — in the `LAST EXCEPTION` alternative, `LAST` is underlined and `EXCEPTION` is
    NOT (measured: LAST underlined at y=456.7 x=235.5, EXCEPTION plain at x=263.3). The LaTeX underlined BOTH.
    The identical phrase in the page-661 raising-phrase had it RIGHT, so the two disagreed with each other; the
    measurement settles which. ⚠ This is recorded AS PRINTED and flagged: an un-underlined `EXCEPTION` there is
    hard to read as a true optional word, and it may be a defect in the standard's own typesetting. Recording
    ISO's defects rather than silently "correcting" them is the standing rule.

The other six diagrams were already semantically correct and are converted for form only.

    python scripts/spec/repairs/latex_diagrams_to_house_style.py --dry-run
    python scripts/spec/repairs/latex_diagrams_to_house_style.py
"""
from __future__ import annotations

import argparse
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parents[3]
SPEC = REPO / "specs" / "ISO_COBOL.md"

NOTE = "> **Figure notes ({what}).** Underlining measured from the printed page (`scripts/spec/figure_extract.py`)."


def block(fig: str, note: str) -> str:
    return "```\n" + fig.strip("\n") + "\n```\n\n" + note


# (unique substring identifying the LaTeX line, replacement block)
EDITS: list[tuple[str, str]] = [
    # ---- page 322, ALPHABET clause FOR phrase --------------------------------------------------------------
    ("\\left[\\text{FOR} \\left\\{ \\begin{array}{l} \\underline{\\text{ALPHANUMERIC}}", block(
        """
⎡ FOR ⎧ ALPHANUMERIC ⎫ ⎤
⎣     ⎩ NATIONAL     ⎭ ⎦
""",
        "> **Figure notes (ALPHABET clause, FOR phrase).** Underlining measured from the printed page: "
        "`ALPHANUMERIC` and `NATIONAL` are underlined (required words, 5.2.2); `FOR` is **not** underlined and "
        "is therefore an optional word per 5.2.3. The brace is a plain select-exactly-one (5.2.6.3) — no choice "
        "indicators are printed — and the enclosing bracket makes the whole phrase optional.")),

    # ---- page 322, SYMBOLIC CHARACTERS clause --------------------------------------------------------------
    ("\\text{ \\{ symbolic-character-1 \\} ... }", block(
        """
⎧ { symbolic-character-1 } …   ⎡ IS  ⎤   { integer-1 } … ⎫ …
⎩                              ⎣ ARE ⎦                   ⎭
""",
        "> **Figure notes (SYMBOLIC CHARACTERS clause).** Underlining measured from the printed page: neither "
        "`IS` nor `ARE` is underlined, so both are optional words per 5.2.3 and the bracket around them may be "
        "omitted entirely. `SYMBOLIC` and `CHARACTERS`, which introduce the clause, are underlined in the "
        "printed heading of the format. The trailing `…` repeats the whole braced group.")),

    # ---- page 606, ACCEPT Format 2 (temporal) --------------------------------------------------------------
    ("\\underline{\\text{ACCEPT}} \\text{ identifier-2 } \\underline{\\text{FROM}}", block(
        """
                              ⎧ DATE [ YYYYMMDD ] ⎫
                              ⎪ DAY [ YYYYDDD ]   ⎪
ACCEPT identifier-2 FROM      ⎨ DAY-OF-WEEK       ⎬   [ END-ACCEPT ]
                              ⎩ TIME              ⎭
""",
        "> **Figure notes (ACCEPT Format 2, temporal).** Underlining measured from the printed page: `ACCEPT`, "
        "`FROM`, `DATE`, `YYYYMMDD`, `DAY`, `YYYYDDD`, `DAY-OF-WEEK`, `TIME` and `END-ACCEPT` are all underlined "
        "(required words, 5.2.2).\n"
        "> ⚠ **`YYYYMMDD` and `YYYYDDD` are underlined**, which an earlier transcription of this figure missed. "
        "They sit inside optional brackets, so each phrase may be omitted — but if it is written, the word "
        "itself is required. Leaving them un-underlined would instead have implied, per 5.2.3, that the word "
        "may be dropped while the bracket is still written.\n"
        "> The brace is a plain select-exactly-one (5.2.6.3): no choice indicators are printed, so exactly one "
        "of the four temporal sources shall be specified."
        # The source line carried the NEXT format's label after the LaTeX, so replacing the whole line drops it
        # — and page 607's figure is then left with no "Format 3" heading at all. Re-emit it.
        "\n\nFormat 3 (screen):")),

    # ---- page 649, EVALUATE range-expression ---------------------------------------------------------------
    ("\\text{arithmetic-expression-3}\\end{array}\\right\\}\\left\\{\\begin{array}{l}\\underline{\\text{THROUGH}}",
     block(
        """
⎧ identifier-3            ⎫ ⎧ THROUGH ⎫ ⎧ identifier-4            ⎫
⎨ literal-3               ⎬ ⎨ THRU    ⎬ ⎨ literal-4               ⎬ [ IN alphabet-name-1 ]
⎩ arithmetic-expression-3 ⎭ ⎩         ⎭ ⎩ arithmetic-expression-4 ⎭
""",
        "> **Figure notes (range-expression).** Underlining measured from the printed page: `THROUGH` and `THRU` "
        "are underlined (required words, 5.2.2) and are equivalent — 12.3.7.3 syntax rule 12 says so explicitly. "
        "`IN` is **not** underlined, so it is an optional word per 5.2.3. All three braces are plain "
        "select-exactly-one (5.2.6.3); no choice indicators are printed.")),

    # ---- page 653, EXIT Format 2 (program) -----------------------------------------------------------------
    ("\\underline{\\text{EXIT}}\\ \\underline{\\text{PROGRAM}}\\ \\left[\\ \\underline{\\text{RAISING}}", block(
        """
                       ⎡         ⎧ EXCEPTION exception-name-1 ⎫ ⎤
EXIT PROGRAM           ⎢ RAISING ⎨ identifier-1               ⎬ ⎥
                       ⎣         ⎩ LAST EXCEPTION             ⎭ ⎦
""",
        "> **Figure notes (EXIT Format 2, program).** Underlining measured from the printed page: `EXIT`, "
        "`PROGRAM`, `RAISING`, `LAST`, and the **first** `EXCEPTION` are underlined (required words, 5.2.2).\n"
        "> ⚠ **In the `LAST EXCEPTION` alternative, `EXCEPTION` is NOT underlined** (measured: `LAST` underlined "
        "at x=235.5, `EXCEPTION` plain at x=263.3), unlike the `EXCEPTION exception-name-1` alternative above "
        "it. An earlier transcription of this figure underlined both, which disagreed with the identical "
        "raising-phrase printed on page 661. It is recorded here AS PRINTED and flagged rather than silently "
        "harmonised: an un-underlined `EXCEPTION` is hard to read as a true optional word, and may be a defect "
        "in the standard's own typesetting — but a reader must be able to check this note against the page.\n"
        "> The brace is a plain select-exactly-one (5.2.6.3); the bracket makes the whole RAISING phrase "
        "optional.")),

    # ---- page 653, EXIT Format 4 (procedure) ---------------------------------------------------------------
    ("\\underline{\\text{EXIT}}\\ \\left\\{\\begin{array}{l} \\underline{\\text{PARAGRAPH}}", block(
        """
     ⎧ PARAGRAPH ⎫
EXIT ⎩ SECTION   ⎭
""",
        "> **Figure notes (EXIT Format 4, procedure).** Underlining measured from the printed page: `EXIT`, "
        "`PARAGRAPH` and `SECTION` are all underlined (required words, 5.2.2). The brace is a plain "
        "select-exactly-one (5.2.6.3): exactly one of `PARAGRAPH` or `SECTION` shall be specified.")),

    # ---- page 661, raising-phrase --------------------------------------------------------------------------
    ("\\underline{\\text{RAISING}} \\left\\{ \\begin{array}{l} \\underline{\\text{EXCEPTION}}", block(
        """
        ⎧ EXCEPTION exception-name-1 ⎫
RAISING ⎨ identifier-1               ⎬
        ⎩ LAST EXCEPTION             ⎭
""",
        "> **Figure notes (raising-phrase).** Underlining measured from the printed page: `RAISING`, `LAST` and "
        "the **first** `EXCEPTION` are underlined (required words, 5.2.2); the `EXCEPTION` in `LAST EXCEPTION` "
        "is **not** (measured: `LAST` underlined at x=142.8, `EXCEPTION` plain at x=170.0). This matches the "
        "printed page and the same phrase in EXIT Format 2 on page 653 — see the flag on that figure. The brace "
        "is a plain select-exactly-one (5.2.6.3).")),

    # ---- page 661, status-phrase ---------------------------------------------------------------------------
    ("\\text{WITH} \\left\\{ \\begin{array}{l} \\underline{\\text{ERROR}}", block(
        """
     ⎧ ERROR  ⎫        ⎡ identifier-2 ⎤
WITH ⎩ NORMAL ⎭ STATUS ⎣ literal-1    ⎦
""",
        "> **Figure notes (status-phrase).** Underlining measured from the printed page: `ERROR` and `NORMAL` "
        "are underlined (required words, 5.2.2); `WITH` and `STATUS` are **not**, so both are optional words per "
        "5.2.3. The brace is a plain select-exactly-one (5.2.6.3) and the bracket makes the operand optional.")),
]


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    lines = SPEC.read_text(encoding="utf-8").splitlines()

    # Locate everything first, then rewrite back-to-front so no index shifts under a later edit.
    targets = []
    for needle, repl in EDITS:
        idxs = [i for i, l in enumerate(lines) if needle in l]
        if len(idxs) != 1:
            sys.exit(f"FATAL: expected exactly 1 line containing {needle[:52]!r}, found {len(idxs)}")
        targets.append((idxs[0], repl, needle))

    for i, repl, needle in sorted(targets, reverse=True):
        print(f"  line {i + 1:>6}  {needle[:56]}…")
        lines[i] = repl

    print(f"\n{len(targets)} LaTeX diagram(s) converted to house style")
    if args.dry_run:
        print("DRY RUN — nothing written.")
        return 0

    text = "\n".join(lines) + "\n"
    SPEC.write_text(text, encoding="utf-8", newline="\n")

    if "\\left" in text:
        n = sum(1 for l in text.splitlines() if "\\left" in l)
        sys.exit(f"FATAL: {n} LaTeX diagram line(s) still present — every general format should now be ASCII")
    fenced, fence = [], False
    for i, l in enumerate(text.split("\n")):  # LF only, so i + 1 is the file's line number
        if l.lstrip().startswith("```"):
            fence = not fence
            continue
        if fence and "<u>" in l:
            fenced.append(i + 1)
    if fenced:
        sys.exit(f"FATAL: <u> tags inside code fences render literally: lines {fenced}")
    print(f"wrote {SPEC.relative_to(REPO)} — no LaTeX general formats remain")
    return 0


if __name__ == "__main__":
    sys.exit(main())
