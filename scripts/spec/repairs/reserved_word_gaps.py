#!/usr/bin/env python3
"""Split the three run-together lines in §8.9, and gate the list against the printed page.

WHAT WAS WRONG. Eighteen reserved words — OCCURS, OF, THAN, TALLYING, TERMINATE, TEST, TABLE, SYSTEM-DEFAULT,
NUMERIC-EDITED, OBJECT/-COMPUTER/-REFERENCE and six END-* verbs — were not on lines of their own. They sat
COLLAPSED onto three lines, six to a line:

    END-PERFORM END-RECEIVE END-READ END-RETURN END-REWRITE END-SEARCH

which is a printed COLUMN read across instead of down. Every word was present, so a word count could never
find it; what was lost was that each is a separate entry — and the run-together line also puts END-RECEIVE
before END-READ, so the list is not even in the order it claims.

⚠ HOW THIS WAS NEARLY MADE WORSE. Found by diffing the section against `ReservedWords.Table.cs` (410 flagged
ISO-2023, section yielding 392) and independently against the printed pages; both named the same 18. Reading
"missing" literally, the first version of this script INSERTED all eighteen — duplicating every one, because
they were never absent. Caught by the owner reading the applied diff. `reserved_word_list.py` carries a
word-conservation check and would have refused; this script had none, which is exactly why it could not see
that the words it was "restoring" were already on the page. It now has one, and it is the reason the repair
is a SPLIT rather than an insert.

    python scripts/spec/repairs/reserved_word_gaps.py            # report
    python scripts/spec/repairs/reserved_word_gaps.py --apply
    python scripts/spec/repairs/reserved_word_gaps.py --check    # gate: section == printed page
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[3]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"
PDF_DIR = REPO / "specs-private"
HEAD = re.compile(r"^#{2,}\s+8\.9\s+Reserved words\s*$")
NEXT = re.compile(r'^<a id="section-8-10')
WORD = re.compile(r"^[A-Za-z][A-Za-z0-9-]*$")
LIST_PAGES = range(234, 238)          # 0-based PDF pages carrying the alphabetic list

# Printed form → transcribed form. Both are ISO's own defects, not ours; C1 in the Addendum covers EMD-START.
PRINTED_QUIRKS = {"EMD-START": "END-START"}
WRAP_TAILS = {"QUIET", "SIGNALING"}   # continuation fragments already rejoined into their stem


def printed_words():
    import fitz                                       # imported late: only the PDF-backed modes need it
    pdf = next(PDF_DIR.glob("*.pdf"), None)
    if pdf is None:
        sys.exit("FATAL: the licensed PDF is absent — git submodule update --init specs-private")
    doc = fitz.open(pdf)
    rows = []
    for pno in LIST_PAGES:
        for b in doc[pno].get_text("dict")["blocks"]:
            if b["type"] != 0:
                continue
            for ln in b["lines"]:
                t = "".join(s["text"] for s in ln["spans"]).strip()
                if WORD.match(t) and t not in ("E", "NOTE"):
                    rows.append((pno, round(ln["bbox"][0]), round(ln["bbox"][1]), t))
    rows.sort(key=lambda r: (r[0], r[1], r[2]))       # page, then column, then line — the reading order
    return {PRINTED_QUIRKS.get(t, t) for _, _, _, t in rows if t not in WRAP_TAILS}


def section(lines):
    s = next(i for i, l in enumerate(lines) if HEAD.match(l))
    e = next(i for i in range(s + 1, len(lines)) if NEXT.match(lines[i]))
    return s, e


def items(lines):
    """The word ENTRIES — one per line item. A run-together line contributes nothing here, which is the
    difference this script exists to close."""
    s, e = section(lines)
    return [t for l in lines[s + 1:e] if WORD.match(t := re.sub(r"^\s*-\s+", "", l).strip())]


def split_runs(lines):
    """Give every word on a run-together line an item of its own, in place."""
    s, e = section(lines)
    body, n = [], 0
    for l in lines[s + 1:e]:
        t = l.strip()
        parts = t.split()
        if len(parts) > 1 and all(WORD.match(p) for p in parts) and not t.startswith(("-", ">", "#")):
            body += [f"- {p}" for p in parts]
            n += 1
            continue
        body.append(l)
    return lines[:s + 1] + body + lines[e:], n


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--check", action="store_true", help="gate: the section's words equal the printed page's")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    lines = SPEC_MD.read_text(encoding="utf-8").splitlines()
    out, n = split_runs(lines)

    # ⛔ The check whose ABSENCE let the first version duplicate eighteen words: a split may only move a word
    # onto its own line, never add or drop one. Bullets are stripped at line start, so the only tokens
    # compared are the words themselves.
    def bag(ls):
        return collections.Counter(w for l in ls for w in re.sub(r"^\s*- ", "", l).split())
    before, after = bag(lines), bag(out)
    if before != after:
        sys.exit(f"FATAL: word conservation failed — lost {dict(before - after)}, "
                 f"gained {dict(after - before)}")

    have, want = set(items(out)), printed_words()
    missing, extra = want - have, have - want
    print(f"{n:6}  run-together lines split")
    print(f"printed {len(want)}  ·  transcription entries {len(have)}")
    if extra:
        print(f"⚠ in the transcription but NOT printed ({len(extra)}): {sorted(extra)}")
    if missing:
        print(f"⚠ MISSING ({len(missing)}): {sorted(missing)}")

    if args.check:
        cur = set(items(lines))
        bad = (want - cur) | (cur - want)
        if bad or n:
            print(f"\nCHECK FAILED — §8.9 differs from the printed page ({len(bad)} words, {n} run-together lines)")
            return 1
        print("\nCHECK: clean — §8.9 carries exactly the printed reserved words, one per entry")
        return 0
    if not n:
        print("nothing to split")
        return 0
    if not args.apply:
        print("\n(report only — pass --apply to write)")
        return 0
    SPEC_MD.write_text("\n".join(out) + "\n", encoding="utf-8", newline="\n")
    print(f"\napplied to {SPEC_MD.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
