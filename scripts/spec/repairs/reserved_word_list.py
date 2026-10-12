#!/usr/bin/env python3
"""Rebuild §8.9's reserved-word list as a real list, and stop Markdown eating the special-character words.

WHAT WAS WRONG. The 406 reserved words were transcribed one per line with no list marker. Markdown joins
consecutive lines into ONE PARAGRAPH, so the printed three-column list rendered as run-on prose — the same
defect the TOC, the index and the figure lists had, which is why those are nested lists now. This one was
missed because `lint_rendering`'s RUN-ON LIST check keys on a cross-reference link per line, and a word list
has no links.

⛔ AND THE FIFTEEN SPECIAL-CHARACTER WORDS WERE DESTROYED OUTRIGHT, each by a different rule:

    +        became an EMPTY BULLET      — `+` is a Markdown bullet marker
    >   >>   VANISHED                    — opened a blockquote; the characters ARE the marker
    >=       rendered as `=`             — same, with the `=` surviving inside the quote
    <   <>   became an <h1>              — the `=` on the NEXT line read as a setext heading underline
    **       ran together with its neighbours in the joined paragraph

So a normative list of COBOL's special-character words rendered with five of them missing and two others
turned into a heading. Every one is now a list item whose content is INLINE CODE — `` `>>` `` — which is both
what they are (a character sequence, not prose) and immune to re-parsing, needing no escape soup.

ONE TRANSCRIPTION DEFECT REPAIRED. The subtract operator was transcribed as an EN DASH (U+2013). The printed
glyph is the arithmetic minus and the character in COBOL's repertoire is HYPHEN-MINUS; no COBOL program
contains U+2013. The PDF text layer returns U+FFFD here (an unmapped glyph — see PDF-TEXT-LAYER.md), so the
en dash was a guess, and the same list on printed folio 215 extracts a plain `-`. The evidence it had already
caused damage downstream: `gen-reserved-words.ps1` carries `.Replace([char]0x2013, '-')` to undo it.

NOT REPAIRED, DELIBERATELY — `i-O` and `I-OICONTROL` are printed exactly so (rendered and inspected at 600
dpi). They are ISO's own typesetting defects, not ours, and the transcription reproduces the printed page.
They are Addendum candidates, an owner call, not a silent edit.

    python scripts/spec/repairs/reserved_word_list.py            # report
    python scripts/spec/repairs/reserved_word_list.py --apply
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[3]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"

HEAD = re.compile(r"^#{2,}\s+8\.9\s+Reserved words\s*$")
NEXT = re.compile(r'^<a id="section-8-10')
WORD = re.compile(r"^[A-Za-z][A-Za-z0-9-]*$")           # `i-O` is printed lower-case; match it as printed
EN_DASH = "–"


def rebuild(lines):
    start = next(i for i, l in enumerate(lines) if HEAD.match(l))
    end = next(i for i in range(start + 1, len(lines)) if NEXT.match(lines[i]))

    out, changed, dash = [], 0, 0
    for i in range(start + 1, end):
        raw = lines[i]
        t = raw.strip()
        if not t or t.startswith(("#", "<a id", "-", "NOTE", "The following")) or len(t.split()) > 1:
            out.append(raw)                              # prose, the NOTE, blanks — untouched
            continue
        if t == EN_DASH:
            t = "-"                                      # the subtract operator, as printed and as COBOL has it
            dash += 1
        t = t.replace("\\*", "*").replace("\\>", ">")    # undo the earlier per-character escapes; code spans win
        # A word is plain text; a special-character word is a character SEQUENCE, so it is inline code — which
        # is also the only form no Markdown rule can re-parse.
        out.append(f"- {t}" if WORD.match(t) else f"- `{t}`")
        changed += 1
    return lines[:start + 1] + out + lines[end:], changed, dash


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    lines = SPEC_MD.read_text(encoding="utf-8").splitlines()
    out, changed, dash = rebuild(lines)

    def words(ls):
        """Every word a reader ends up with. The bullet is stripped at LINE START only — a global `- `
        replacement also eats the subtract operator, which is itself a list item here. The en dash is folded
        to the hyphen on BOTH sides so the one deliberate character repair does not read as a lost word,
        which is exactly what it did on the first run."""
        toks = []
        for l in ls:
            s = re.sub(r"^\s*- ", "", l).replace("\\", "").replace("`", "")
            toks += s.replace(EN_DASH, "-").split()
        return collections.Counter(toks)

    before, after = words(lines), words(out)
    if before != after:
        sys.exit(f"FATAL: word conservation failed — lost {before - after}, gained {after - before}")

    print(f"{changed:6}  reserved words made list items")
    print(f"{dash:6}  en dash → hyphen-minus (the subtract operator)")
    print(f"{sum(before.values()):,} words conserved ({len(before):,} distinct)")
    if not args.apply:
        print("\n(report only — pass --apply to write)")
        return 0
    SPEC_MD.write_text("\n".join(out) + "\n", encoding="utf-8", newline="\n")
    print(f"applied to {SPEC_MD.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
