#!/usr/bin/env python3
"""Validate a spec citation against `specs/ISO_COBOL.md` — the mechanical check for CLAUDE.md rule 1.

WHY THIS EXISTS. Rule 1 requires the exact §/GR for any semantics question, and the failure mode is not
inventing a citation — it is INHERITING one. A fix-queue entry or design doc carries "§8.4.1.2 GR2", the text
it quotes really does say what it claims, and the clause NUMBER is never re-derived; it then propagates into
code comments, goldens and DEVLOG entries as though it had been checked. Two of the CA10 citations were wrong
this way (the real clauses are §8.4.2.3.4 GR2 and §13.18.38.4 GR7) and nothing would have caught them.

⛔ IT READS THE MARKDOWN, NOT `spec-rule-catalog.json`. The catalog is derived and has two properties that make
it the wrong oracle here: it has no block at all for a clause whose rules are normative PROSE with lettered
items (§14.8.2.3.3 is one — checking against the catalog reports a real citation as false), and its `ordinal`
is a per-block count that does NOT track the printed rule number once a rule has sub-letters (the abs rule
prints as 6)d)2.b) and the catalog calls it 2). Both produce confident wrong answers.

    python scripts/spec/cite.py --check 8.4.2.3.4 "EC-BOUND-SUBSCRIPT exception condition is set to exist"
    python scripts/spec/cite.py --find  "shall fall within the bounds from integer-1"

`--check` exits non-zero unless the text appears INSIDE that clause's own region, so a wrong clause number
cannot pass quietly. That is the load-bearing guarantee. Both modes also print a best-effort RULE PATH
(`6) d)`) to help you write the ordinal — ⚠ it is APPROXIMATE below two levels of nesting, because the standard
mixes `a)` and `b.` styles at the same depth; read the printed rule when the ordinal itself is load-bearing.

⛔ A CHECK THAT CANNOT BE VALID FAILS, IT NEVER ANSWERS OK (kb/Work PB308). The comparison is on WORDS ONLY
(`norm`: punctuation is typography), so a quotation made entirely of punctuation — a table row such as
`| + | + | - |` — normalizes to the EMPTY string, which is a substring of every line of every clause. Both modes
refuse such a quotation outright; it is the gate failing open, which is worse than the gate failing.

THE CORRECTED TEXT AND THE PRINTED TEXT BOTH PASS (R66, specs/README.md "Corrections"). Where the transcription
corrects a defect of the printed standard, the corrected line is followed by a `> ⚠ **CORRECTED — see the
Addendum (Cn).**` note that quotes the PRINTED line verbatim, so a quotation of either form is found inside the
same clause, and the rule path reported for a quotation of the printed form is the corrected line's.
"""
from __future__ import annotations

import argparse
import functools
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC = REPO / "specs" / "ISO_COBOL.md"

# A clause number is either the body's dotted decimal (14.9.39.4) or an ANNEX's letter-headed form
# (E.2, A.4.14, D.2.2.5.1). The annex alternative requires at least one dot segment, which is what keeps it
# from swallowing the annex TITLE headings ("## Annex A") — those name the annex, they are not a clause.
# ⛔ Without the second alternative the whole of Annexes A–G is uncitable: `--check E.2 …` answers
# "there is no clause §E.2", which reads as a bad citation rather than a blind tool.
# The TITLE is optional: all 178 headings of clause 3 (Terms and definitions) are a bare number, the term
# itself being the bold line beneath. Requiring a title made every definition in the standard uncitable.
HEADING = re.compile(r"^#{2,6}\s+([0-9]+(?:\.[0-9]+)*|[A-Z](?:\.[0-9]+)+)(?:\s+(.*?))?\s*$")
# The transcription escapes a rule label's delimiter (`1\)`) so Markdown does not eat it as a list — see
# repairs/rule_numbering.py. Match both forms so this works on any revision.
TOP = re.compile(r"^(\d+)\\?\)\s")
SUB = re.compile(r"^(\s{2,})([a-z])\\?\)\s")
SUBSUB = re.compile(r"^(\s{2,})(\d+)\\?\.\s")
#: An editorial note on a correction (R66): the paragraph belongs to the line ABOVE it, so a quotation found in
#: the note reports that line's rule path.
CORRECTION_NOTE = re.compile(r"^>\s*⚠\s*\*\*CORRECTED")

EMPTY_NEEDLE = "quotation carries no word characters (letters, digits, underscore); nothing to match"


@functools.lru_cache(maxsize=1 << 17)  # > the transcription's ~47,500 lines
def norm(s: str) -> str:
    """Compare on words only — dashes, quotes and spacing are typography, not content.

    Memoized, because an in-process caller checks many quotations against ONE loaded standard: `find` normalizes
    every line of the spec per call, and `audit_doc_citations.py` (kb/Work PB2525) spent 25 of its 33 s re-running
    these two regular expressions over the same ~47,500 lines for 59 `find` calls."""
    return re.sub(r"\s+", " ", re.sub(r"[^\w\s]", " ", s)).strip().lower()


def clauses(lines):
    """(number, title, start, end) for every clause heading, in order."""
    heads = [(m.group(1), m.group(2) or "", i) for i, l in enumerate(lines) if (m := HEADING.match(l))]
    return [(n, t, s, heads[k + 1][2] if k + 1 < len(heads) else len(lines))
            for k, (n, t, s) in enumerate(heads)]


def _indent(line: str) -> int:
    return len(line) - len(line.lstrip(" "))


def rule_path(lines, at, start):
    """The PRINTED rule path of the line at `at` — e.g. ['6)', 'd)', '2.'] — by walking back to each level.

    Two lines are not what their position suggests, and each used to report a sub-item it is not in (PB308):
      · an editorial note on a correction belongs to the line above it, so it takes that line's path;
      · an UNLETTERED paragraph at the sub-items' own indentation (or shallower) is a trailing paragraph of the
        PARENT rule — §14.7.7 rule 2's "The composite of operands is a hypothetical data item…" follows its
        sub-items a) and b) at their indent and is neither — so it reports the bare rule, not the last letter.
    A paragraph INSIDE a sub-item sits deeper than its label, which is how the two are told apart."""
    while at > start and CORRECTION_NOTE.match(lines[at]):
        at -= 1
        while at > start and not lines[at].strip():
            at -= 1
    own = lines[at]
    is_label = bool(TOP.match(own) or SUB.match(own) or SUBSUB.match(own))
    own_indent = _indent(own)
    path, seen_sub, seen_subsub = [], False, False
    for k in range(at, start - 1, -1):
        line = lines[k]
        if not seen_subsub and (m := SUBSUB.match(line)):
            if is_label or own_indent > len(m.group(1)):
                path.append(f"{m.group(2)}.")
            seen_subsub = True
        elif not seen_sub and (m := SUB.match(line)):
            if is_label or own_indent > len(m.group(1)):
                path.append(f"{m.group(2)})")
            seen_sub = True
        elif (m := TOP.match(line)):
            path.append(f"{m.group(1)})")
            break
    return " ".join(reversed(path))


def find(lines, cls, text):
    """(exit code, output lines) for `--find`: every clause line that contains TEXT (words only)."""
    needle = norm(text)
    if not needle:
        return 1, [f"REFUSED: {EMPTY_NEEDLE}"]
    hits = [(n, t, s, e, i) for (n, t, s, e) in cls
            for i in range(s, e) if needle in norm(lines[i])]
    if not hits:
        return 1, [f"NO CLAUSE contains {text!r} — the citation names text the standard does not have"]
    out = []
    for n, t, s, _e, i in hits[:12]:
        out.append(f"§{n} {rule_path(lines, i, s)}  ({t})\n    {lines[i].strip()[:190]}\n")
    out.append(f"{len(hits)} occurrence(s)")
    return 0, out


def check(lines, cls, clause, text):
    """(exit code, output lines) for `--check`: TEXT must appear inside CLAUSE's own region."""
    region = [c for c in cls if c[0] == clause]
    if not region:
        return 1, [f"FAIL: there is no clause §{clause} in the transcription"]
    needle = norm(text)
    if not needle:
        return 1, [f"FAIL: {EMPTY_NEEDLE}"]
    for n, t, s, e in region:
        for i in range(s, e):
            if needle in norm(lines[i]):
                return 0, [f"OK  §{n} {rule_path(lines, i, s)}  ({t})\n    {lines[i].strip()[:220]}"]
    return 1, [f"FAIL: §{clause} exists but does NOT contain {text!r}",
               "      run --find to locate the text and get the clause it is really in"]


def load(path: pathlib.Path = SPEC):
    lines = path.read_text(encoding="utf-8").splitlines()
    return lines, clauses(lines)


def self_test() -> int:
    """Each guard is shown to FIRE on its defect and to stay SILENT on its neighbour (kb/Work PB308, PB309)."""
    lines, cls = load()
    cases = [
        ("fires EMPTY-NEEDLE (check)", check(lines, cls, "7.1", "| + | + | - |")[0] == 1),
        ("fires EMPTY-NEEDLE (find)", find(lines, cls, "| + | - |")[0] == 1),
        ("silent on a needle carrying digits", check(lines, cls, "15.43.4", "| S999 | +999 |")[0] == 0),
        ("pins 14.9.8.4 1) b)", "1) b)" in check(lines, cls, "14.9.8.4", "Otherwise, arithmetic-expression-1 is evaluated to produce an algebraic value")[1][0]),
        ("pins 14.7.7 2) bare (trailing paragraph)", check(lines, cls, "14.7.7", "The composite of operands is a hypothetical data item")[1][0].startswith("OK  §14.7.7 2)  (")),
        ("pins 8.8.3.3 3)", check(lines, cls, "8.8.3.3", "equivalent to a literal")[1][0].startswith("OK  §8.8.3.3 3)")),
        ("accepts the CORRECTED form (C6)", check(lines, cls, "14.9.10.4", "The value '62' is placed into the I-O status")[0] == 0),
        ("accepts the PRINTED form (C6)", check(lines, cls, "14.9.10.4", "The valu62' is placed into the I-O status")[0] == 0),
        ("accepts the PRINTED form (C12)", check(lines, cls, "14.9.30.4", "whose key value is greater than or equal to the key value in the file position indicator")[0] == 0),
        ("fires on a wrong clause", check(lines, cls, "14.9.27.4", "The valu62' is placed into the I-O status")[0] == 1),
    ]
    for name, ok in cases:
        print(f"{'PASS' if ok else 'FAIL'}  {name}")
    bad = [n for n, ok in cases if not ok]
    print("SELF-TEST: " + ("FAIL " + str(bad) if bad else "PASS"))
    return 1 if bad else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--check", nargs=2, metavar=("CLAUSE", "TEXT"),
                    help="assert TEXT appears inside CLAUSE's own region; exit 1 if not")
    ap.add_argument("--find", metavar="TEXT", help="which clause(s) contain this text")
    ap.add_argument("--self-test", action="store_true", help="prove each guard fires and stays silent")
    # before parse_args: `--help` prints the module docstring, whose characters a cp1252 console cannot encode
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    if not (args.find or args.check):
        ap.print_help()
        return 2
    lines, cls = load()
    code, out = find(lines, cls, args.find) if args.find else check(lines, cls, *args.check)
    print("\n".join(out))
    return code


if __name__ == "__main__":
    sys.exit(main())
