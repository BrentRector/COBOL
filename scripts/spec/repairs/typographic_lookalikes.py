#!/usr/bin/env python3
"""Normalize the typographic look-alikes the transcription uses for the standard's own PUNCTUATION (kb/Work PB291).

THE DEFECT. The PDF's text layer spells several of the standard's ASCII characters with look-alikes, and the
transcription carried them over verbatim. They print identically to `-`, which is why they survived review, but
the CHARACTER is wrong: §8.3.2.1 makes the hyphen-minus (U+002D) the only hyphen a COBOL word can contain, so
`integer<U+2011>1` is not the word `integer-1` the standard defines, and the same holds for the arithmetic
operator and the PICTURE symbol. Every consumer that matches with an ASCII hyphen — a `\\bidentifier-1\\b` pattern,
`cite.py`, a human's grep — silently matches NOTHING in such a rule (the under-selection direction: nothing goes red).

WHAT IS NORMALIZED, per class (the class is listed ONCE in the Addendum, C14):
    U+2011 NON-BREAKING HYPHEN   107  always a hyphen a COBOL word or name contains         -> `-`
    U+2212 MINUS SIGN             34  always the arithmetic operator / PICTURE symbol / sign   -> `-`
    U+2013 EN DASH               103  MIXED, decided per occurrence by `classify` below:
        normalized   where it stands for the COBOL hyphen-minus: a quoted `'–'`, a PICTURE symbol list or table
                     cell, `the operator –`, a unary sign before a numeral, and a binary subtraction between
                     operands in the normative clauses;
        KEPT         a numeric RANGE (`1–6`, `807–815`), a list dash that opens a line, and every prose dash
                     (`a single character – either …`). Annexes E–G carry no COBOL text, so every en dash there is kept.

⛔ A `<pre>` figure (a general format) is NEVER rewritten: `sweep_figures.py --check` regenerates those from the PDF
and diffs them, and a figure's own characters are that tool's to decide. Nor is the Addendum.

The repair may change NO letter, digit or word: the conservation check compares the text with every one of the
three characters mapped to `-` on both sides, so all it can differ by is which of the three a hyphen was spelled with.
Idempotent. `--apply` writes; the default reports every KEPT en dash so the keep list can be read.

    python scripts/spec/repairs/typographic_lookalikes.py            # report
    python scripts/spec/repairs/typographic_lookalikes.py --apply
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[3]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"

NB_HYPHEN, MINUS, EN_DASH = "‑", "−", "–"
#: Annex D (Concepts) onward is prose about the language, not COBOL text: only the unambiguous forms apply there.
PROSE_FROM_HEADING = "## Annex D"
#: Annexes E-G (substantive changes, archaic lists, known errors) and the Addendum carry no COBOL text at all.
NO_COBOL_FROM_HEADING = "## Annex E"

QUOTED = re.compile(r"'" + EN_DASH + r"'")
SYMBOL_CELL = re.compile(r"(?:\|\s*|\*\*|`)" + EN_DASH + r"(?:\s*\||\*\*|`)")
SYMBOL_LIST = re.compile(r"\+\s+" + EN_DASH + r"\s+(?:CR|DB)|\+ " + EN_DASH + r" CR DB|the operator " + EN_DASH
                         + r"|and the symbols `\+` and `" + EN_DASH + r"`|MINUS and \*\*" + EN_DASH + r"\*\*")
UNARY = re.compile(r"(?<![\w)\]])" + EN_DASH + r"(?=\s?(?:\d|p/2|\())")
UNARY_BOLD = re.compile(r"\*\*" + EN_DASH + r"\*\*(?=\s?(?:\d|p/2))")
RANGE = re.compile(r"\d" + EN_DASH + r"\d")


def classify(line: str, at: int, in_prose_annex: bool) -> tuple[bool, str]:
    """(normalize?, why) for the en dash at `line[at]`."""
    if RANGE.search(line[max(0, at - 1):at + 2]):
        return False, "range"
    if line.lstrip().startswith(EN_DASH + " ") and line.index(EN_DASH) == at:
        return False, "list dash"
    window = line[max(0, at - 30):at + 30]
    rel = at - max(0, at - 30)
    for pat, why in ((QUOTED, "quoted"), (SYMBOL_CELL, "symbol cell"), (SYMBOL_LIST, "symbol list")):
        if any(m.start() <= rel < m.end() for m in pat.finditer(window)):
            return True, why
    for pat in (UNARY, UNARY_BOLD):
        if any(m.start() <= rel < m.end() for m in pat.finditer(window)):
            return True, "unary sign"
    if in_prose_annex:
        return False, "prose (annex)"
    # a binary subtraction: operands around it are identifiers, numerals or parenthesized expressions
    lo = line.rfind(" ", 0, at - 1) + 1 if at > 0 else 0
    left = line[lo:at].strip()
    right = line[at + 1:].lstrip().split(" ", 1)[0]
    if re.fullmatch(r"[A-Za-z]{2,}", left) and re.fullmatch(r"[A-Za-z]{2,}", right):
        return False, "prose"      # (a one-letter operand — `n - j + 1` — is a variable, not a word)
    if not right or right[0] in "\"'“":
        return False, "prose"
    return True, "binary subtraction"


def repair(lines):
    """-> (new lines, counters, kept [(line number, why, context)])."""
    out, counts, kept = [], collections.Counter(), []
    in_pre = in_fence = False
    in_prose_annex = no_cobol = in_addendum = False
    for n, line in enumerate(lines, start=1):
        s = line.strip()
        if s.startswith("<pre"):
            in_pre = True
        elif s == "</pre>":
            in_pre = False
        elif s.startswith("```"):
            in_fence = not in_fence
        if line.startswith(PROSE_FROM_HEADING):
            in_prose_annex = True
        if line.startswith(NO_COBOL_FROM_HEADING):
            no_cobol = True
        if 'id="addendum"' in line:
            in_addendum = True
        if in_pre or in_fence or in_addendum:
            out.append(line)
            for ch in (NB_HYPHEN, MINUS, EN_DASH):
                if ch in line:
                    counts[f"kept, in a figure/addendum: U+{ord(ch):04X}"] += line.count(ch)
            continue
        new = line
        for ch, name in ((NB_HYPHEN, "U+2011 NON-BREAKING HYPHEN"), (MINUS, "U+2212 MINUS SIGN")):
            k = new.count(ch)
            if k:
                counts[name] += k
                new = new.replace(ch, "-")
        if EN_DASH in new:
            chars = list(new)
            for at, ch in enumerate(new):
                if ch != EN_DASH:
                    continue
                if no_cobol:
                    kept.append((n, "prose (annex E-G)", new[max(0, at - 25):at + 25]))
                    counts["U+2013 EN DASH kept (prose, annex E-G)"] += 1
                    continue
                normalize, why = classify(new, at, in_prose_annex)
                if normalize:
                    chars[at] = "-"
                    counts[f"U+2013 EN DASH -> `-` ({why})"] += 1
                else:
                    kept.append((n, why, new[max(0, at - 25):at + 25]))
                    counts[f"U+2013 EN DASH kept ({why})"] += 1
            new = "".join(chars)
        out.append(new)
    return out, counts, kept


def fold(text: str) -> str:
    return text.replace(NB_HYPHEN, "-").replace(MINUS, "-").replace(EN_DASH, "-")


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    src = SPEC_MD.read_text(encoding="utf-8")
    lines = src.split("\n")
    out, counts, kept = repair(lines)
    if fold("\n".join(out)) != fold(src):
        sys.exit("FATAL: conservation failed — the repair changed something other than the spelling of a dash")
    for name, n in sorted(counts.items()):
        print(f"{n:6}  {name}")
    for n, why, ctx in kept:
        print(f"   kept {n:6} [{why}]  ...{ctx}...")
    if not args.apply:
        print("\n(report only — pass --apply to write)")
        return 0
    SPEC_MD.write_text("\n".join(out), encoding="utf-8")
    print(f"applied to {SPEC_MD.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
