#!/usr/bin/env python3
"""Remove the duplicated printed-page-546 block stranded inside the page-575 slice.

THE DEFECT. The body of printed page 546 (the object-destruction NOTE, §14.6.11 items 5-7, all of §14.6.12
Abnormal run unit termination, and §14.6.13 / .1 / .1.1) is emitted TWICE: once stranded at the end of the
page-575 slice, introduced by a stray horizontal rule and a stray running-header H1, and again in its proper place
under the `page-576` anchor. Four section anchors are therefore emitted twice, and because the stray copy comes
FIRST in file order, every intra-document link and every heading-derived TOC entry resolves to the copy that sits
under the WRONG page anchor. A grep-based citation workflow gets two hits for one rule with no way to tell which
is canonical — and that is precisely how the `spec-lookup` skill reads this file.

WHICH COPY SURVIVES. The one under `<a id="page-576">`. The stray copy has no page anchor of its own, so any
citation resolved through it reports the wrong printed page.

WHY THIS IS SCRIPTED RATHER THAN HAND-EDITED. Deleting the wrong range loses normative content silently. Every
boundary is asserted against its expected text before a single line is removed, so a shifted line aborts the run
instead of shifting the deletion.

A whole-file duplicate scan found exactly ONE artifact of this kind. Eleven other repeated runs were checked and
left alone: ten are distant repetitions separated by several page anchors, and one (the INSPECT worked examples)
is two examples legitimately sharing a code-block header and table header.

    python scripts/spec/repairs/duplicate_block_p576.py --dry-run
    python scripts/spec/repairs/duplicate_block_p576.py
"""
from __future__ import annotations

import argparse
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[3]
SPEC = REPO / "specs" / "ISO_COBOL.md"

# The four section anchors the stray copy re-emits. After the repair each must appear exactly once.
DUPED_ANCHORS = [
    'id="section-14-6-12"', 'id="section-14-6-13"',
    'id="section-14-6-13-1"', 'id="section-14-6-13-1-1"',
]


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    lines = SPEC.read_text(encoding="utf-8").splitlines(keepends=True)

    # Locate by CONTENT, never by hard-coded line number — the heading-depth repair already shifted this file
    # once, and a future repair will shift it again.
    anchor576 = next((i for i, l in enumerate(lines) if l.strip() == '<a id="page-576"></a>'), None)
    if anchor576 is None:
        sys.exit("FATAL: page-576 anchor not found")

    # Walk back from the anchor to the last line of genuine page-575 content.
    keep_end = None
    for i in range(anchor576 - 1, anchor576 - 60, -1):
        if lines[i].strip() == "4) All instance objects are destroyed.":
            keep_end = i
            break
    if keep_end is None:
        sys.exit("FATAL: the page-575 tail marker was not found where expected")

    stray = lines[keep_end + 1:anchor576]
    stray_text = "".join(stray)

    # Assert the stray region is what we think it is BEFORE removing it.
    for expect in ("---", "# ISO/IEC 1989:2023 (E)"):
        if expect not in stray_text:
            sys.exit(f"FATAL: expected stray marker {expect!r} not present — boundaries have moved, aborting")
    for a in DUPED_ANCHORS:
        if stray_text.count(a) != 1:
            sys.exit(f"FATAL: expected exactly one {a} in the stray copy, found {stray_text.count(a)}")
    if any(l.strip().startswith("<a id=\"page-") for l in stray):
        sys.exit("FATAL: the stray region contains a PAGE anchor — it is not the stray copy, aborting")

    print(f"page-575 content ends at line {keep_end + 1}")
    print(f"page-576 anchor at line       {anchor576 + 1}")
    print(f"stray region                  {keep_end + 2}-{anchor576} ({len(stray)} lines)")
    print(f"  non-blank lines removed     {sum(1 for l in stray if l.strip())}")

    if args.dry_run:
        print("\nDRY RUN — nothing written.")
        return 0

    # Keep exactly one blank line between the page-575 tail and the page-576 anchor.
    lines[keep_end + 1:anchor576] = ["\n"]
    SPEC.write_text("".join(lines), encoding="utf-8", newline="\n")

    text = "".join(lines)
    bad = [a for a in DUPED_ANCHORS if text.count(a) != 1]
    if bad:
        sys.exit(f"FATAL: after repair these anchors are still not unique: {bad}")
    pages = len(re.findall(r'^<a id="page-\d+"></a>', text, re.M))
    print(f"\nwrote {SPEC.relative_to(REPO)}")
    print(f"  the four section anchors are now unique")
    print(f"  page anchors: {pages} (must be 1261)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
