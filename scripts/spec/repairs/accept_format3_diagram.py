#!/usr/bin/env python3
"""Replace the ACCEPT Format 3 LaTeX diagrams (page 607) with a measured, house-style transcription.

WHY THIS ONE WAS HELD BACK. It was the ninth of nine exception-phrase figures that had lost their choice
indicators, but unlike the other eight it is rendered as LaTeX, and its two independent verifiers CONTRADICTED
each other about the AT phrase: one reported no brackets around the individual LINE / COLUMN rows, the other
reported `[ AT {| [LINE NUMBER ...] / [{COLUMN|COL} NUMBER ...] |} ]` — with brackets. Guessing between them
would have written a fabricated structure into the reference document.

MEASUREMENT SETTLES IT. `figure_geometry.py` reports every bracket stem on the page, and in the AT band there are
exactly two — the outer pair at x=92.16 / x=348.01 (h=113.21) — plus the separate ON EXCEPTION pair lower down.
There are NO bracket stems around the LINE row or the COLUMN row. The first verifier was right. What sits just
inside the big brace is a pair of CHOICE-INDICATOR BARS (x=122.98 / x=337.97, h=97.34, bare rules with no feet).

WHAT THE PRINTED FIGURE ACTUALLY IS:

    outer BRACKET          the whole AT phrase is optional
      AT                   outside the brace, and NOT underlined - an optional word per 5.2.3
      BRACE + BARS         one or more of the LINE and COLUMN phrases, each at most once, IN ANY ORDER
        LINE NUMBER  { identifier-3 / integer-1 }
        { COLUMN / COL } NUMBER { identifier-4 / integer-2 }

WHAT THE TRANSCRIPTION SAID INSTEAD — a brace containing two BRACKETED rows, which reads as "exactly one of
(optional LINE phrase) or (optional COLUMN phrase)". That is wrong in both directions at once: it forbids
`AT LINE NUMBER 5 COLUMN NUMBER 10`, the ordinary form, and it permits an empty AT with nothing after it.

UNDERLINING, read off the page by `figure_extract.py` rather than asserted:
    underlined (required, 5.2.2):  LINE, COLUMN, COL, EXCEPTION, NOT, END-ACCEPT
    NOT underlined (optional, 5.2.3):  AT, NUMBER, ON
The transcription underlined COLUMN but not COL — although COL is the required abbreviation, printed underlined —
and dropped the underlining from the ON EXCEPTION rows entirely.

    python scripts/spec/repairs/accept_format3_diagram.py --dry-run
    python scripts/spec/repairs/accept_format3_diagram.py
"""
from __future__ import annotations

import argparse
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parents[3]
SPEC = REPO / "specs" / "ISO_COBOL.md"

AT_OLD_PREFIX = "$$\\left[\\begin{array}{l} \\text{AT} \\left\\{"
EXC_OLD = ("$$\\left[\\begin{array}{l} \\text{ON EXCEPTION imperative-statement-1} \\\\ "
           "\\text{NOT ON EXCEPTION imperative-statement-2} \\end{array}\\right]$$")

AT_NEW = """```
⎡      ⎧|                                ⎧ identifier-3 ⎫      |⎫ ⎤
⎢      ⎪|   LINE NUMBER                  ⎩ integer-1    ⎭      |⎪ ⎥
⎢  AT  ⎨|                                                      |⎬ ⎥
⎢      ⎪|   ⎧ COLUMN ⎫                   ⎧ identifier-4 ⎫      |⎪ ⎥
⎣      ⎩|   ⎩ COL    ⎭  NUMBER           ⎩ integer-2    ⎭      |⎭ ⎦
```

> **Figure notes (ACCEPT Format 3, AT phrase).** Transcribed from measurements of the printed page, not by eye:
> the delimiters are vector rectangles in the PDF and were read directly (`scripts/spec/figure_geometry.py`), as
> was the underlining (`scripts/spec/figure_extract.py`).
> ⚠ **The brace carries CHOICE INDICATORS** — a pair of bars just inside it (measured at x=122.98 and x=337.97,
> bare rules with no bracket feet, spanning both rows). Per 5.2.6.4 that means **one or more** of the enclosed
> alternatives shall be specified, each at most once, **in any order**: the LINE phrase alone, the COLUMN phrase
> alone, or **both, in either order**. It is NOT a select-exactly-one brace.
> There are **no brackets around the individual LINE and COLUMN rows** — the only bracket stems in this band are
> the outer pair, which makes the whole AT phrase optional. An earlier reading that bracketed each row would have
> forbidden `AT LINE NUMBER 5 COLUMN NUMBER 10` while permitting an empty `AT`.
> Underlined in the printed figure (required words, 5.2.2): `LINE`, `COLUMN`, `COL`. Not underlined (optional
> words, 5.2.3): `AT`, `NUMBER` — so `ACCEPT screen-name-1 LINE NUMBER 5` is legal without the word `AT`."""

EXC_NEW = """```
⎡|  ON EXCEPTION imperative-statement-1      |⎤
⎣|  NOT ON EXCEPTION imperative-statement-2  |⎦
```

> **Figure notes (ISO 5.2.6.4).** The `ON EXCEPTION` / `NOT ON EXCEPTION` group is enclosed in BRACKETS WITH
> CHOICE INDICATORS (measured: bracket stems at x=90.76 / x=322.10 with feet, bare bars at x=95.90 / x=316.73).
> Per 5.2.6.4 that means zero or more of the enclosed alternatives may be specified, each at most once, in any
> order — so one ACCEPT statement may specify neither phrase, either alone, or **both, in either order**.
> Underlined in the printed figure: `EXCEPTION` (both occurrences) and `NOT`; `ON` is **not** underlined and is
> therefore an optional word per 5.2.3."""


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    lines = SPEC.read_text(encoding="utf-8").splitlines()

    at_idx = [i for i, l in enumerate(lines) if l.startswith(AT_OLD_PREFIX)]
    exc_idx = [i for i, l in enumerate(lines) if l.strip() == EXC_OLD]
    if len(at_idx) != 1:
        sys.exit(f"FATAL: expected exactly 1 AT-phrase LaTeX line, found {len(at_idx)}")
    if len(exc_idx) != 1:
        sys.exit(f"FATAL: expected exactly 1 ON EXCEPTION LaTeX line, found {len(exc_idx)}")
    if not at_idx[0] < exc_idx[0]:
        sys.exit("FATAL: the two blocks are not in the expected order")

    print(f"  AT phrase     line {at_idx[0] + 1}  -> measured house-style figure + notes")
    print(f"  ON EXCEPTION  line {exc_idx[0] + 1}  -> bracket with choice indicators + notes")
    if args.dry_run:
        print("\nDRY RUN — nothing written.")
        return 0

    lines[exc_idx[0]] = EXC_NEW          # replace the LATER one first, so the earlier index stays valid
    lines[at_idx[0]] = AT_NEW
    text = "\n".join(lines) + "\n"
    SPEC.write_text(text, encoding="utf-8", newline="\n")

    if "\\left[\\begin{array}{l} \\text{ON EXCEPTION" in text:
        sys.exit("FATAL: the ON EXCEPTION LaTeX block survived the rewrite")
    if "\\text{COL}" in text:
        sys.exit("FATAL: the un-underlined COL still appears — the AT block did not go")
    print(f"\nwrote {SPEC.relative_to(REPO)} — ACCEPT Format 3 now matches the measured page")
    return 0


if __name__ == "__main__":
    sys.exit(main())
