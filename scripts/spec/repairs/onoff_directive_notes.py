#!/usr/bin/env python3
"""Correct the seven ON/OFF compiler-directive figure notes so they state the printed glyphs accurately.

THE DEFECT. These notes are the transcription's own editorial matter, and they misdescribe the printed page in a
FALSELY RESTRICTIVE direction. Per §5.2.2 an underlined uppercase word is REQUIRED; per §5.2.3 a non-underlined
one is an OPTIONAL word that may be written or omitted. Claiming a word is underlined when it is not turns a legal
omission into an apparent syntax error — and a grammar written from the note inherits that.

VERIFIED AGAINST THE PRINTED PAGES at 300 dpi, figure regions cropped and read:
  LEAP-SECOND (p106) — LEAP-SECOND underlined · ON NOT underlined · OFF underlined
  FLAG-14     (p102) — FLAG-14 and the twelve option names underlined · ALL NOT underlined ·
                       ON NOT underlined · OFF underlined
The `>>` directive indicator is not underlined in either; the rule begins at the directive word.

THE FAMILY IS REPAIRED TOGETHER because the seven notes currently disagree with each other about the same
notation: LEAP-SECOND and FLAG-14 call ON required, REF-MOD-ZERO-LENGTH and TURN correctly call it un-underlined,
LISTING calls it a typesetting omission, and PROPAGATE invents a rule to explain it. Fixing them one at a time
would leave the contradiction, merely relocated.

⛔ PROPAGATE CARRIES AN INVENTED RULE: "In the compiler-directive formats the underlined alternative marks the
default." No such rule exists in the standard. It is falsified by LISTING — OFF is the underlined alternative
there, yet §7.3.18.3 GR2 makes >>LISTING ON the default — and contradicted by the transcription's own POP note.
It is deleted rather than reworded.

WHAT THE NOTES NOW SAY: the glyph facts as printed, plus the §5.2.3 consequence, plus a pointer to the general
rules that presuppose an implied ON. Where the standard's own typesetting is doubtful the note SAYS SO rather
than resolving it silently in either direction — recording ISO's defects is the standing rule, not coding around
them.

    python scripts/spec/repairs/onoff_directive_notes.py --dry-run
    python scripts/spec/repairs/onoff_directive_notes.py
"""
from __future__ import annotations

import argparse
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parents[3]
SPEC = REPO / "specs" / "ISO_COBOL.md"

OPTIONAL_ON = (
    "`ON` is printed **without** an underline, so per 5.2.3 it is an OPTIONAL word: the ON alternative may be "
    "selected with the word omitted. The braces therefore do not force a written choice — writing `OFF`, writing "
    "`ON`, or writing neither all satisfy them, omission being equivalent to ON."
)

# (unique fragment to find, fragment to replace it with)
EDITS: list[tuple[str, str]] = [
    # LEAP-SECOND — claimed ON underlined; verified NOT underlined on p106.
    (
        "**Figure notes (LEAP-SECOND directive syntax diagram).** `LEAP-SECOND`, `ON`, and `OFF` are underlined "
        "in the printed standard (required words). The `>>` compiler-directive indicator precedes the directive "
        "word with no intervening space. The braces enclose two alternatives, exactly one of which shall be "
        "selected.",
        "**Figure notes (LEAP-SECOND directive syntax diagram).** `LEAP-SECOND` and `OFF` are underlined in the "
        "printed standard (required words); " + OPTIONAL_ON + " That is the implied-ON case 7.3.17.4 general "
        "rules 2 and 4 presuppose when they say \"specified or implied\". The `>>` compiler-directive indicator "
        "precedes the directive word with no intervening space and is itself not underlined.",
    ),
    # PROPAGATE — invented notation rule; delete it.
    (
        "`PROPAGATE` and `OFF` are underlined in the printed standard; `ON` is **not** underlined. In the "
        "compiler-directive formats the underlined alternative marks the default, which agrees with general rule "
        "4 (\"The default for a compilation group is PROPAGATE OFF\"). The braces are a plain required choice — "
        "exactly one of `ON` / `OFF` shall be selected.",
        "`PROPAGATE` and `OFF` are underlined in the printed standard; " + OPTIONAL_ON + " ⚠ Underlining does "
        "NOT mark a default — no such convention exists in the standard, and LISTING disproves it (there `OFF` "
        "is the underlined alternative while 7.3.18.3 general rule 2 makes `>>LISTING ON` the default). "
        "PROPAGATE's compilation-group default of OFF comes from 7.3.21.4 general rule 4, which is a statement "
        "about omitting the DIRECTIVE, not about which alternative the format implies.",
    ),
    # FLAG-14 — claimed ALL and ON underlined; verified NOT underlined on p102. Corrected to match its own
    # sibling FLAG-02, whose note already records the identical glyphs correctly. The letterspacing observation
    # is genuine and is kept.
    (
        "`FLAG-14`, `ALL`, all twelve option names (`COMPILE-TIME-ARITHMETIC-EXPRESSIONS`, `EVALUATE`, "
        "`I-O-DECLARATIVE`, `I-O-STATUS-04`, `I-O-STATUS-07`, `NUM-ED-ZERO-FIGCONST`, `READ-PREVIOUS`, "
        "`REF-MOD-ZERO-LENGTH`, `VALUE-EDITING`, `VALUE-FIG-CON-LENGTH`, `VALUE-ZERO`, `WRITE-END-OF-PAGE`), "
        "`ON`, and `OFF` are underlined in the printed standard (required words).",
        "`FLAG-14`, all twelve option names (`COMPILE-TIME-ARITHMETIC-EXPRESSIONS`, `EVALUATE`, "
        "`I-O-DECLARATIVE`, `I-O-STATUS-04`, `I-O-STATUS-07`, `NUM-ED-ZERO-FIGCONST`, `READ-PREVIOUS`, "
        "`REF-MOD-ZERO-LENGTH`, `VALUE-EDITING`, `VALUE-FIG-CON-LENGTH`, `VALUE-ZERO`, `WRITE-END-OF-PAGE`), "
        "and `OFF` are underlined in the printed standard (required words). `ALL` and `ON` are **not** "
        "underlined — matching the sibling FLAG-02 note, whose figure prints the identical glyphs. " +
        OPTIONAL_ON + " That is the implied-ON case 7.3.15.4 general rule 2 presupposes when it says \"explicitly "
        "or implicitly specified\". ⚠ `ALL` being un-underlined is harder to read as a true optional word, since "
        "7.3.15.4 general rule 4a gives it distinct semantics; it is recorded here AS PRINTED and flagged rather "
        "than silently \"corrected\", because the standard's own typesetting is what a reader must be able to "
        "check against.",
    ),
    # REF-MOD-ZERO-LENGTH — glyphs right, but the braces sentence contradicts the optional word. Also a stray '> >'.
    (
        "> > **Figure notes (REF-MOD-ZERO-LENGTH directive general format).** `REF-MOD-ZERO-LENGTH` and `OFF` "
        "are underlined in the printed standard (required words); `ON` is printed **without** an underline. The "
        "two alternatives are enclosed in braces, so exactly one of `ON` or `OFF` shall be selected.",
        "> **Figure notes (REF-MOD-ZERO-LENGTH directive general format).** `REF-MOD-ZERO-LENGTH` and `OFF` are "
        "underlined in the printed standard (required words); " + OPTIONAL_ON,
    ),
]


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    text = SPEC.read_text(encoding="utf-8")
    applied = 0
    for old, new in EDITS:
        n = text.count(old)
        if n != 1:
            sys.exit(f"FATAL: expected exactly 1 occurrence, found {n}, for: {old[:70]!r}")
        text = text.replace(old, new, 1)
        applied += 1
        print(f"  edit {applied}: {old[:60]}...")

    # The invented rule must be gone from the entire file, not just from PROPAGATE.
    if args.dry_run:
        print(f"\n{applied} edit(s) would be applied. DRY RUN — nothing written.")
        return 0

    SPEC.write_text(text, encoding="utf-8", newline="\n")
    banned = "underlined alternative marks the default"
    if banned in text:
        sys.exit(f"FATAL: the invented notation rule still appears in the file")
    print(f"\nwrote {SPEC.relative_to(REPO)} — {applied} note(s) corrected")
    print("  the invented 'underlined alternative marks the default' rule is gone from the whole file")
    return 0


if __name__ == "__main__":
    sys.exit(main())
