#!/usr/bin/env python3
"""Tag the transcription's COBOL example blocks as ```cobol.

WHY. The standard's informative annexes are full of example programs, and a fenced block with no language tag
renders as undifferentiated monospace — the reader cannot tell a COBOL example from a table of code points or
a fragment of a general format. The tag is also what lets any downstream tool find the example corpus.

WHAT IS AND IS NOT COBOL HERE. A block is tagged when it shows a COBOL *structural* signal — a division or
`END PROGRAM`-style terminator, an IDENTIFICATION-paragraph header, a statement verb, a level-numbered data
description, or an inline `*>` comment.

⚠ EXCEPT when the block is mostly METAVARIABLES. `SEARCH table AT END imperative-statement-1` reads as a
statement and is not source: it is a fragment of a general format, where `imperative-statement-1` stands for
code rather than being code. A block more than half of whose lines carry `identifier-N` / `literal-N` /
bracket-and-brace notation is left untagged.

Blocks left untagged are as important as the ones tagged: the reference-format ruler, the exception-name/status
tables, lists of PICTURE strings, and the general-format fragments are all fenced too, and none is COBOL.

    python scripts/spec/tag_cobol_blocks.py --report
    python scripts/spec/tag_cobol_blocks.py --apply
"""
from __future__ import annotations

import argparse
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"

DIVISION = re.compile(r"^\s*(IDENTIFICATION|ENVIRONMENT|DATA|PROCEDURE)\s+DIVISION\b", re.I)
TERMINATOR = re.compile(r"^\s*END\s+(PROGRAM|FUNCTION|CLASS|INTERFACE|METHOD|FACTORY|OBJECT)\b", re.I)
PARAGRAPH = re.compile(
    r"^\s*(PROGRAM-ID|FUNCTION-ID|CLASS-ID|INTERFACE-ID|METHOD-ID|SPECIAL-NAMES|FILE-CONTROL|SOURCE-COMPUTER"
    r"|OBJECT-COMPUTER|REPOSITORY|CONFIGURATION SECTION|WORKING-STORAGE SECTION|LOCAL-STORAGE SECTION"
    r"|LINKAGE SECTION|FILE SECTION|REPORT SECTION|SCREEN SECTION|FACTORY|OBJECT)\s*\.", re.I)
STATEMENT = re.compile(
    r"^\s*(MOVE|DISPLAY|PERFORM|COMPUTE|EVALUATE|CALL|INVOKE|SET|ADD|SUBTRACT|MULTIPLY|DIVIDE|STRING|UNSTRING"
    r"|INSPECT|SEARCH|SORT|MERGE|OPEN|CLOSE|READ|WRITE|REWRITE|DELETE|ACCEPT|GOBACK|STOP\s+RUN|INITIALIZE"
    r"|RAISE|IF|END-IF|END-PERFORM|END-EVALUATE|SEND|RECEIVE|SELECT|RELEASE|RETURN|CANCEL|ALLOCATE|FREE"
    r"|VALIDATE|TERMINATE|GENERATE|INITIATE|SUPPRESS|PURGE|ENABLE|DISABLE|USE|EXIT\s+(PROGRAM|METHOD|FUNCTION)"
    r"|LOCK\s+MODE|ASSIGN)\b", re.I)
# A LEVEL NUMBER may be written with or without its leading zero (`1` and `01` are the same level), which the
# single-digit alternative has to allow for without swallowing the separating space.
LEVEL = re.compile(
    r"^\s*(0[1-9]|[1-9]|[1-4][0-9]|66|77|88)\s+[A-Za-z][A-Za-z0-9\-]*\s*"
    r"(\.|PIC|PICTURE|USAGE|OBJECT|TYPEDEF|GROUP-USAGE|VALUE|REDEFINES|OCCURS"
    r"|TYPE|LINES?|COLUMNS?|COL|SOURCE|SUM|GROUP|CONSTANT)", re.I)
# A REPORT-WRITER entry may omit the data-name entirely — `05  LINE + 2.` is a complete report group
# description line — so the level number is followed by the CLAUSE rather than by a name.
RW_LEVEL = re.compile(r"^\s*(0[1-9]|[1-9]|[1-4][0-9]|66|77|88)\s+(TYPE|LINES?|COLUMNS?|COL|SOURCE|SUM|GROUP|VALUE|PIC|PICTURE|USAGE)", re.I)
# FD / SD / RD / CD open a file, sort-merge, report or communication description entry — all COBOL source.
ENTRY = re.compile(r"^\s*(FD|SD|RD|CD)\s+[A-Za-z]", re.I)
# A COMPILER DIRECTIVE is COBOL source too — it is written in the source file and read by the compiler.
DIRECTIVE = re.compile(r"^\s*>>[A-Z]", re.I)
METAVARIABLE = re.compile(r"imperative-statement-\d|identifier-\d|literal-\d|data-name-\d|[{}\[\]]")


def signals(body):
    found = set()
    for line in body:
        if (DIVISION.match(line) or TERMINATOR.match(line) or ENTRY.match(line)
                or DIRECTIVE.match(line)):
            found.add("structure")
        if PARAGRAPH.match(line):
            found.add("paragraph")
        if STATEMENT.match(line):
            found.add("statement")
        if LEVEL.match(line) or RW_LEVEL.match(line):
            found.add("level")
        if "*>" in line:
            found.add("comment")
    return found


def blocks_of(lines):
    out, start = [], None
    for i, l in enumerate(lines):
        if l.startswith("```"):
            if start is None:
                start = i
            else:
                out.append((start, i))
                start = None
    return out


def is_cobol(body):
    sig = signals(body)
    if not sig:
        return False
    meta = sum(1 for l in body if METAVARIABLE.search(l))
    return meta <= len(body) / 2


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--report", action="store_true")
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    lines = SPEC_MD.read_text(encoding="utf-8").splitlines()
    tagged = skipped = already = 0
    for a, b in blocks_of(lines):
        if lines[a].strip() != "```":
            already += 1
            continue
        if is_cobol(lines[a + 1:b]):
            lines[a] = lines[a].replace("```", "```cobol", 1)
            tagged += 1
        else:
            skipped += 1

    print(f"fenced blocks        : {tagged + skipped + already}")
    print(f"  tagged as cobol    : {tagged}")
    print(f"  left untagged      : {skipped}   (rulers, tables, PICTURE lists, general-format fragments)")
    print(f"  already tagged     : {already}")
    if args.apply:
        SPEC_MD.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
        print(f"\napplied to {SPEC_MD}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
