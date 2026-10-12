#!/usr/bin/env python3
"""Repair heading depth in specs/ISO_COBOL.md against the PDF's embedded outline.

WHY THE OUTLINE IS THE AUTHORITY. Heading level is nowhere in the printed page: the body does not distinguish
levels typographically (a §13.18.41.4 heading is set identically to §13.18.40.4), and the printed Contents pages
carry no indentation — every entry sits flush at the same left margin. But the PDF carries an EMBEDDED OUTLINE of
2,090 entries with explicit levels 1–5, and that is the document's own statement of its hierarchy.

Measured before the repair: of 1,724 markdown headings matched to an outline entry, only **57 (3%)** sat at the
right depth. The markdown was essentially flat while the standard nests five deep. The 19 heading findings the
reconciliation sweep confirmed were symptoms of that, and their verifiers normalised each to its LOCAL siblings —
which is why they proposed `###` for §10.6.3 where the outline says `####`. Local consistency is not depth.

MAPPING: markdown level = outline level + 1. The offset exists because `#` is already taken by the transcription's
running header (`# ISO/IEC 1989:2023 (E)`). Outline levels 1–5 therefore map to `##`–`######`, exactly filling
markdown's six levels with no overflow.

COVERAGE: every numbered heading is repaired, not only those the outline lists. The outline omits 374 leaves, and
repairing §15.7 while leaving §15.7.1 behind would swap one inconsistency for another. This is sound because the
outline's level equals the dotted depth for all 1,728 numbered entries — verified, zero exceptions — so depth is a
PROVEN proxy rather than an assumption, and the script asserts the equality still holds on every matched heading
rather than trusting the measurement to survive a future spec revision.

Page markers, the running header and Contents links are never touched.

    python scripts/spec/repairs/heading_depth.py --dry-run   # report, change nothing
    python scripts/spec/repairs/heading_depth.py             # apply
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[3]
SPEC = REPO / "specs" / "ISO_COBOL.md"
PDF = REPO / "specs-private" / "ISO+IEC+1989-2023_ for X_952804 COBOL.pdf"

# A numbered section heading. Deliberately NOT matching "## Page 27" (no dot-number form beyond a bare integer is
# fine, but page markers are excluded by requiring the title to follow the number) or the running header.
HEADING = re.compile(r"^(#{1,6})\s+(?P<num>[A-Z]?\d+(?:\.\d+)*)\s+(?P<title>\S.*?)\s*$")
PAGE_MARKER = re.compile(r"^#{1,6}\s+Page\s+\d+\s*$", re.I)


def authoritative_levels() -> dict[str, int]:
    import fitz  # PyMuPDF

    out: dict[str, int] = {}
    for lvl, title, _ in fitz.open(str(PDF)).get_toc():
        m = re.match(r"^([A-Z]?\d+(?:\.\d+)*)\s", title)
        if m:
            # First occurrence wins: a section number is unique in the outline, and a later duplicate would be
            # a defect in the outline rather than a reason to overwrite a good level.
            out.setdefault(m.group(1), lvl)
    return out


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    auth = authoritative_levels()
    print(f"authoritative sections from the PDF outline: {len(auth)}")

    lines = SPEC.read_text(encoding="utf-8").splitlines(keepends=True)
    changed = 0
    matched = 0
    unmatched: list[str] = []
    shapes: collections.Counter = collections.Counter()

    for i, line in enumerate(lines):
        if PAGE_MARKER.match(line) or line.lstrip().startswith("["):
            continue
        m = HEADING.match(line)
        if not m:
            continue
        num = m.group("num")
        # The outline's level EQUALS the dotted depth for all 1,728 numbered entries — verified, zero
        # exceptions. So depth is a proven proxy, which matters because 374 markdown headings have no outline
        # entry at all (the outline omits some leaves). Using the outline alone would repair §15.7 and leave
        # §15.7.1 behind, replacing one inconsistency with another. Where an entry EXISTS the two agree by
        # construction; the assertion below keeps it that way rather than trusting the measurement to hold.
        depth = num.count(".") + 1
        if num in auth:
            matched += 1
            if auth[num] != depth:
                sys.exit(f"FATAL: §{num} outline level {auth[num]} != dotted depth {depth} — "
                         "the proxy no longer holds; re-derive before repairing")
        else:
            unmatched.append(f"{num} {m.group('title')[:50]}")
        want = depth + 1                          # '#' is the transcription's running header
        if want > 6:                              # cannot happen with levels 1-5, but never silently truncate
            sys.exit(f"FATAL: §{num} needs markdown level {want}, beyond the 6 markdown provides")
        have = len(m.group(1))
        if have == want:
            continue
        shapes[("#" * have, "#" * want)] += 1
        lines[i] = "#" * want + line[have:]
        changed += 1

    total = matched + len(unmatched)
    verb = "would rewrite" if args.dry_run else "rewrote"
    print(f"numbered headings processed                : {total}")
    print(f"  outline-verified (level == dotted depth) : {matched}")
    print(f"  depth-only (no outline entry)            : {len(unmatched)}")
    print(f"  already at the right depth               : {total - changed}")
    print(f"  {verb:<41}: {changed}")
    print("  examples with no outline entry (still repaired, by depth):")
    for u in unmatched[:10]:
        print(f"     {u}")
    print("\n  shapes (have -> want):")
    for (h, w), n in shapes.most_common(10):
        print(f"     {h} -> {w}   {n}")

    if args.dry_run:
        print("\nDRY RUN — nothing written.")
        return 0

    SPEC.write_text("".join(lines), encoding="utf-8", newline="\n")
    print(f"\nwrote {SPEC.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
