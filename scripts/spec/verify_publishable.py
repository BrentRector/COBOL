#!/usr/bin/env python3
"""Pre-publication check for the spec transcription: no per-copy licence data, and the acknowledgment present.

WHY THIS EXISTS. `specs/ISO_COBOL.md` is published; the PDF it was transcribed from is NOT. Two things therefore
have to be true of the Markdown before it leaves the private submodule, and neither is guaranteed by any other
gate:

  1. **It carries no per-copy licence stamp.** Every page of the source PDF is stamped by the reseller with the
     purchaser's name, order number and download date — "Licensed to …. Single user license only. Copying and
     networking prohibited." That is not part of ISO/IEC 1989:2023: it is applied per purchaser, it is personally
     identifying, and its terms are the reseller's, not the standard's. The OCR pass picked it up once, at the
     end of the cover page, where it had been concatenated onto the genuine "© ISO/IEC 2023" line. One
     occurrence in 53,000 lines is exactly the kind of thing a human review misses and a grep does not.

  2. **The ISO acknowledgment opens the file.** Page 28 of the standard grants reproduction "in whole or in part
     … for any other purpose" and asks that the acknowledgment paragraphs be reproduced "in their entirety as
     part of the preface to any such publication". That request is the condition the transcription is published
     under.

DELIBERATELY RUNS WITHOUT THE PDF. `verify_acknowledgment.py` checks the acknowledgment WORD FOR WORD against the
printed page, and needs the PDF to do it. This check needs to run in the public repository, where the PDF is
absent by design — so it verifies presence and structure only, and defers fidelity to the other gate. The two are
complements, not duplicates.

    python scripts/spec/verify_publishable.py
"""
from __future__ import annotations

import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from cite import CORRECTION_NOTE  # noqa: E402  — ONE definition of what an editorial correction note looks like

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"

# The reseller's per-copy stamp, in pieces, so a partial survival is caught as readily as the whole line.
FORBIDDEN = [
    (r"Licensed to\b", "per-copy licence stamp"),
    (r"Single user licen[cs]e", "per-copy licence stamp"),
    (r"Copying and networking", "per-copy licence stamp"),
    (r"ANSI order\b", "reseller order reference"),
    (r"\bX_\d{6}\b", "reseller order number"),
    (r"Downloaded \d{1,2}/\d{1,2}/\d{4}", "per-copy download date"),
]

# Must be present. The acknowledgment's own opening words, and the preface framing that marks our editorial
# matter as ours.
REQUIRED = [
    (r"^#\s*Preface", "the file must open with the Preface (page 28: 'as part of the preface')"),
    (r"COBOL is an industry language and is not the property of any company",
     "the acknowledgment's first paragraph"),
    (r"No warranty, expressed or implied, is made by any contributor",
     "the acknowledgment's no-warranty paragraph"),
    (r"have specifically authorized the use of this material in whole or in part",
     "the acknowledgment's authorization paragraph"),
    (r"NOT part of ISO/IEC 1989:2023",
     "the Preface must state that it is not part of the standard"),
]


#: An Addendum correction entry: `### C6 · page 607 · 14.9.10.4 … · printed → corrected`.
ENTRY_HEADING = re.compile(r"^###\s+(C\d+)\s+·\s+(.*)$")
FLAG_REFERENCE = re.compile(r"see the Addendum \((C\d+)\)")
PAGES_IN_HEADING = re.compile(r"\bpages?\s+((?:\d+(?:,\s*|\s+and\s+)?)+)")
PAGE_IN_NOTE = re.compile(r"\bpage\s+(\d+)\b")
#: A correction that changes a CLASS of characters across the whole text is flagged once, in the Preface, and is
#: listed under this heading in place of a page (C14: the look-alike hyphens and minus signs).
WHOLE_TEXT = "the whole text"


def check_corrections(text: str) -> list[str]:
    """Every correction is FLAGGED where it was made, LISTED in the Addendum, and names its PRINTED FORM and PAGE.

    The Addendum exists so a correction can be reversed if it later proves mistaken (specs/README.md "Corrections",
    kb/Work R66). That only works while three things hold, and each is checked here because each can be edited away
    independently of the others:

      1. the two sets agree — every `see the Addendum (Cn)` flag has a `### Cn` entry and every entry is pointed at;
      2. every in-place flag is a `> ⚠ **CORRECTED — see the Addendum (Cn).**` note that QUOTES THE PRINTED FORM in a code
         span and names the PRINTED PAGE, and that page is one the entry's heading lists — `cite.py --check` finds a
         quotation of the printed form in the note (and reports the corrected line's rule path, cite.rule_path), which
         is what keeps every citation written against the printed form passing;
      3. every entry's heading names the printed page(s) (or `the whole text` for a class of characters), and an entry
         other than such a class states its printed form.
    """
    failures: list[str] = []
    lines = text.split("\n")
    addendum_at = next((i for i, l in enumerate(lines) if l.startswith("# Addendum")), len(lines))
    print("\ncorrections — every flag must match an Addendum entry that names the printed form and page:")

    # the Addendum's entries, each with its body up to the next heading
    entries: dict[str, dict] = {}
    current = None
    for l in lines[addendum_at:]:
        if m := ENTRY_HEADING.match(l):
            current = entries.setdefault(m.group(1), {"heading": m.group(2), "body": []})
        elif l.startswith("## ") or l.startswith("# "):
            current = None
        elif current is not None:
            current["body"].append(l)

    # the flags in the body: each is a blockquote paragraph opening with the CORRECTED marker
    flags: dict[str, list[tuple[int, str]]] = {}
    i = 0
    while i < addendum_at:
        if CORRECTION_NOTE.match(lines[i]):
            j = i
            while j + 1 < addendum_at and lines[j + 1].startswith(">"):
                j += 1
            paragraph = " ".join(l.lstrip("> ").strip() for l in lines[i:j + 1])
            ids = FLAG_REFERENCE.findall(paragraph)
            for cid in ids[:1]:
                flags.setdefault(cid, []).append((i + 1, paragraph))
            i = j
        i += 1
    referenced = set(FLAG_REFERENCE.findall("\n".join(lines[:addendum_at])))

    if not entries:
        failures.append("the Addendum lists no corrections (### Cn entries)")
        print("  ✗ no correction entries found in the Addendum")
    for cid in sorted(referenced | set(entries), key=lambda c: int(c[1:])):
        problems: list[str] = []
        entry = entries.get(cid)
        if cid not in entries:
            problems.append("referenced from the text, but MISSING from the Addendum")
        elif cid not in referenced:
            problems.append("in the Addendum, but never referenced from the text")
        else:
            heading, body = entry["heading"], "\n".join(entry["body"])
            is_class = WHOLE_TEXT in heading
            listed = {int(n) for n in re.findall(r"\d+", m.group(1))} if (m := PAGES_IN_HEADING.search(heading)) else set()
            if not listed and not is_class:
                problems.append("its heading names no printed page (`page N`) and is not a whole-text class")
            if not is_class and not re.search(r"\*\*Printed|printed form", body, re.I):
                problems.append("it does not state the printed form")
            if is_class and cid in flags:
                problems.append("a whole-text class is flagged once in the Preface, not per occurrence")
            if not is_class and cid not in flags:
                problems.append("its heading is not a whole-text class but no in-place flag carries it")
            for at, paragraph in flags.get(cid, []):
                pages = {int(n) for n in PAGE_IN_NOTE.findall(paragraph)}
                if not re.search(r"`[^`]+`", paragraph):
                    problems.append(f"the flag at line {at} quotes no printed form in a code span")
                if not pages:
                    problems.append(f"the flag at line {at} names no printed page")
                elif listed and not pages & listed:
                    # (a note may also cite the pages that CONFIRM the correction — C1 names three — but one must be
                    # a page the entry's heading lists, so the flag and the entry point at the same printed page)
                    problems.append(f"the flag at line {at} names page {sorted(pages)} but the entry lists {sorted(listed)}")
        if problems:
            for p in problems:
                failures.append(f"{cid}: {p}")
            print(f"  ✗ {cid}: " + "; ".join(problems))
        else:
            print(f"  ✓ {cid}: flagged in place, listed in the Addendum, printed form and page named")
    return failures


def main() -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if not SPEC_MD.exists():
        sys.exit(f"FATAL: {SPEC_MD} not found")
    text = SPEC_MD.read_text(encoding="utf-8")
    lines = text.splitlines()

    failures = []
    print("per-copy licence data — must be ABSENT:")
    for pat, what in FORBIDDEN:
        hits = [i + 1 for i, l in enumerate(lines) if re.search(pat, l)]
        if hits:
            failures.append(f"{what} ({pat}) at line(s) {hits[:5]}")
            print(f"  ✗ {pat:<34} FOUND at {hits[:5]}")
        else:
            print(f"  ✓ {pat:<34} absent")

    print("\nacknowledgment and preface — must be PRESENT:")
    for pat, what in REQUIRED:
        if re.search(pat, text, re.M):
            print(f"  ✓ {what}")
        else:
            failures.append(f"missing: {what}")
            print(f"  ✗ MISSING — {what}")

    failures.extend(check_corrections(text))

    # The acknowledgment has to be in the preface, i.e. before the body starts, not only in position at 0.2.
    # The body is delimited by the FIRST CLAUSE, not by a page anchor: pages were removed from the document
    # (they are a layout artifact of one typesetting and mean nothing in Markdown), so clause 1 Scope is the
    # stable marker for where the front matter ends.
    cut = text.find('<a id="section-1"></a>')
    if cut < 0:
        failures.append("the clause-1 anchor is missing; cannot locate where the front matter ends")
    elif "COBOL is an industry language" not in text[:cut]:
        failures.append("the acknowledgment appears in the body but NOT in the preface")
        print("  ✗ the acknowledgment is not in the preface (it must precede clause 1)")
    else:
        print("  ✓ the acknowledgment precedes the body, as the preface")

    if failures:
        print("\n".join(["", "FATAL — not publishable:"] + [f"  - {f}" for f in failures]))
        return 1
    print(f"\nPUBLISHABLE — {len(lines):,} lines, no per-copy licence data, acknowledgment in the preface")
    print("(word-for-word fidelity of the acknowledgment is verify_acknowledgment.py, which needs the PDF)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
