#!/usr/bin/env python3
"""Generate the Annex B and Annex C tables from the ISO text (kb/Work PB1402).

The ISO standard is the authority for two compile-time tables (CLAUDE.md rule 1):

* Annex B.3 — the characters permitted in a user-defined word, in three POSITION classes: (1) anywhere,
  (2) anywhere but first, (3) anywhere but first or last (§8.1.3.2 GR4; §8.3.2.1).
* Annex C.2 — the uppercase-to-lowercase case mapping that folds COBOL words (§8.1.3.2 GR3 b) and GR4 b)).

Both are given for COBOL 2023; the earlier editions' tables are the 2023 ones with Annex E's recorded changes
reversed — E.2 item 4 (037A deleted, 30FB made medial), E.2 item 14 (two mappings deleted), E.3.3 item 4
(characters newly allowed first), E.3.3 item 5 (characters added) and E.3.3 item 6 (mappings added). The
generator transcribes each list separately, exactly as the standard prints it, so
AnnexBCDriftTests can re-read every list from specs/ISO_COBOL.md and compare.

Usage: python scripts/spec/gen_annex_bc.py [--check]   (--check: exit 1 when a committed table differs)
"""
from __future__ import annotations

import re
import sys
import unicodedata
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPEC = ROOT / "specs" / "ISO_COBOL.md"

# The section boundaries: (name, first line that OPENS the list, the line that CLOSES it). Each opener and closer
# is a literal prefix of one line of specs/ISO_COBOL.md; the drift test uses the same markers.
SECTIONS = [
    ("B3Anywhere", r"1\) The following characters are permitted in a user-defined word.",
     r"2\) The following characters are permitted in a user-defined word except as the start character."),
    ("B3NotFirst", r"2\) The following characters are permitted in a user-defined word except as the start character.",
     r"3\) The following characters are permitted in a user-defined word except as the start or last character."),
    ("B3Medial", r"3\) The following characters are permitted in a user-defined word except as the start or last character.",
     '<a id="section-annex-c"></a>'),
    ("C2Mappings", "### C.2 General case mappings", '<a id="section-annex-d"></a>'),
    ("E2Item4Deleted", r"4\) **Characters permitted in user-defined words.** The following character, represented",
     "   The following character has been changed so it is not allowed for the start or last character"),
    ("E2Item4MadeMedial", "   The following character has been changed so it is not allowed for the start or last character",
     "   Justification:"),
    ("E2Item14Deleted", r"14\) **General case mappings.** The following case mappings have been deleted.", "Justification:"),
    ("E33Item4MadeFirst", r"4\) **Characters permitted in user-defined words.** The following characters have been changed",
     r"5\) **Characters permitted in user-defined words.** The following characters have been added."),
    ("E33Item5Added", r"5\) **Characters permitted in user-defined words.** The following characters have been added.",
     r"6\) **General case mappings.** The following case mappings have been added."),
    ("E33Item6Added", r"6\) **General case mappings.** The following case mappings have been added.",
     r"7\) **Clarification** of exception handling procedures."),
]

RANGE = re.compile(r"(?<![0-9A-Za-z])([0-9A-F]{4,5})(?:-([0-9A-F]{4,5}))?(?![0-9A-Za-z])")
PAIR = re.compile(r"\(([0-9A-F]{4,5}),([0-9A-F]{4,5})\)")
# A line of a list is hex ranges, script-name labels ("Old_Italic:"), NOTE prose or blank — anything else is an
# OCR defect the generator must not silently skip.
LABEL = re.compile(r"^[A-Za-z][A-Za-z_ ]*:$")


def section_lines(lines: list[str], opener: str, closer: str) -> list[str]:
    start = next(i for i, l in enumerate(lines) if l.startswith(opener))
    end = next(i for i in range(start + 1, len(lines)) if lines[i].startswith(closer))
    return lines[start + 1:end]


def ranges_of(body: list[str]) -> list[tuple[int, int]]:
    out: list[tuple[int, int]] = []
    for line in body:
        text = line.strip()
        if not text or text.startswith("NOTE") or LABEL.match(text) or text.startswith("```"):
            continue
        for m in RANGE.finditer(text):
            lo = int(m.group(1), 16)
            hi = int(m.group(2), 16) if m.group(2) else lo
            if hi < lo:
                raise SystemExit(f"descending range {m.group(0)} in: {text}")
            out.append((lo, hi))
        residue = RANGE.sub("", text)
        residue = re.sub(r"[A-Za-z_]+:", "", residue)          # an inline script label ("1E94B Ahom:")
        residue = residue.replace("—", "").replace(",", "").strip()
        if residue and not re.fullmatch(r"[A-Z][A-Z \-]*", residue):   # a character NAME after a code point
            raise SystemExit(f"unparsed residue {residue!r} in: {text}")
    return out


def pairs_of(body: list[str]) -> list[tuple[int, int]]:
    return [(int(a, 16), int(b, 16)) for line in body for a, b in PAIR.findall(line)]


def merge(rs: list[tuple[int, int]]) -> list[tuple[int, int]]:
    rs = sorted(rs)
    out: list[tuple[int, int]] = []
    for lo, hi in rs:
        if out and lo <= out[-1][1] + 1:
            out[-1] = (out[-1][0], max(out[-1][1], hi))
        else:
            out.append((lo, hi))
    return out


def extract() -> dict[str, list[tuple[int, int]]]:
    lines = SPEC.read_text(encoding="utf-8").splitlines()
    data: dict[str, list[tuple[int, int]]] = {}
    for name, opener, closer in SECTIONS:
        body = section_lines(lines, opener, closer)
        data[name] = pairs_of(body) if name.startswith(("C2", "E2Item14", "E33Item6")) else merge(ranges_of(body))
    return data


def plausibility(data: dict[str, list[tuple[int, int]]]) -> list[str]:
    """Flag a code point whose Unicode category cannot be what its list says (an OCR digit slip). Python's
    unicodedata is a newer Unicode than 13.0.0, so this is a sanity net, never the authority."""
    notes: list[str] = []
    for lo, hi in data["B3Anywhere"]:
        for cp in range(lo, hi + 1):
            cat = unicodedata.category(chr(cp))
            if cat not in ("Lu", "Ll", "Lt", "Lm", "Lo", "Nl", "Nd") and cp not in (0x2118, 0x212E, 0x309B, 0x309C):
                notes.append(f"B3Anywhere {cp:04X} has category {cat}")
    for lo, hi in data["B3NotFirst"]:
        for cp in range(lo, hi + 1):
            cat = unicodedata.category(chr(cp))
            if cat not in ("Mn", "Mc", "Nd", "Pc", "Lo", "Lm", "No") and cp not in (0x00B7, 0x0387, 0x19DA):
                notes.append(f"B3NotFirst {cp:04X} has category {cat}")
    for up, low in data["C2Mappings"]:
        if chr(up).lower() != chr(low):
            notes.append(f"C2 ({up:04X},{low:04X}) differs from Unicode's lowercase {ord(chr(up).lower()[0]):04X}")
    return notes


def render(data: dict[str, list[tuple[int, int]]], names: list[str], namespace: str, cls: str, what: str) -> str:
    def fmt(rs: list[tuple[int, int]]) -> str:
        items = [f"(0x{lo:04X}, 0x{hi:04X})" for lo, hi in rs]
        rows = [", ".join(items[i:i + 6]) for i in range(0, len(items), 6)]
        return "\n".join(f"        {r}," for r in rows)

    out = [
        "// <auto-generated>",
        "// Generated by scripts/spec/gen_annex_bc.py — DO NOT EDIT; re-run the script.",
        f"// Source: specs/ISO_COBOL.md — {what}, and the Annex E",
        "// changes that reverse it for the earlier editions. AnnexBCDriftTests re-reads every list from the spec.",
        "// </auto-generated>",
        f"namespace {namespace};",
        "",
        f"public static partial class {cls}",
        "{",
    ]
    docs = {
        "B3Anywhere": "Annex B.3 item 1 (COBOL 2023): permitted anywhere in a user-defined word.",
        "B3NotFirst": "Annex B.3 item 2 (COBOL 2023): permitted except as the start character.",
        "B3Medial": "Annex B.3 item 3 (COBOL 2023): permitted except as the start or last character.",
        "C2Mappings": "Annex C.2 (COBOL 2023): the general case mappings (uppercase, lowercase).",
        "E2Item4Deleted": "Annex E.2 item 4: deleted from Annex B at COBOL 2023 (present before).",
        "E2Item4MadeMedial": "Annex E.2 item 4: made medial-only at COBOL 2023 (permitted anywhere before).",
        "E2Item14Deleted": "Annex E.2 item 14: case mappings deleted at COBOL 2023 (present before).",
        "E33Item4MadeFirst": "Annex E.3.3 item 4: newly permitted as the first character at COBOL 2023 (not first before).",
        "E33Item5Added": "Annex E.3.3 item 5: characters added to Annex B at COBOL 2023 (absent before).",
        "E33Item6Added": "Annex E.3.3 item 6: case mappings added at COBOL 2023 (absent before).",
    }
    for name in names:
        out.append(f"    /// <summary>{docs[name]}</summary>")
        out.append(f"    internal static readonly (int First, int Second)[] {name} =")
        out.append("    [")
        out.append(fmt(data[name]))
        out.append("    ];")
        out.append("")
    out[-1] = "}"
    return "\n".join(out) + "\n"


# Annex B is a COMPILE-TIME fact (which characters a word may hold), so it lives with the edition catalogues; Annex
# C is also needed at RUN time (an externalized name is matched by the same fold, DOC-A.1-219), so it lives in the
# runtime, which both the compiler and every generated program reference.
OUTPUTS = [
    (ROOT / "src" / "Cobol.Net.Editions" / "CobolCharacterRepertoire.Table.cs", "CobolNet.Editions",
     "CobolCharacterRepertoire", "Annex B.3 (characters permitted in user-defined words)",
     ["B3Anywhere", "B3NotFirst", "B3Medial", "E2Item4Deleted", "E2Item4MadeMedial", "E33Item4MadeFirst",
      "E33Item5Added"]),
    (ROOT / "src" / "Cobol.Net.Runtime" / "CobolNames.Table.cs", "CobolNet.Runtime", "CobolNames",
     "Annex C.2 (general case mappings, as (uppercase, lowercase) pairs)",
     ["C2Mappings", "E2Item14Deleted", "E33Item6Added"]),
]


def main() -> int:
    data = extract()
    for note in plausibility(data):
        print("plausibility:", note, file=sys.stderr)
    stale = 0
    for path, namespace, cls, what, names in OUTPUTS:
        text = render(data, names, namespace, cls, what)
        if "--check" in sys.argv:
            current = path.read_text(encoding="utf-8") if path.exists() else ""
            if current != text:
                print(f"{path.relative_to(ROOT)} is stale; re-run scripts/spec/gen_annex_bc.py", file=sys.stderr)
                stale = 1
            continue
        path.write_bytes(text.encode("utf-8"))
    if "--check" not in sys.argv:
        for name, rs in data.items():
            print(f"{name}: {len(rs)} entries")
    return stale


if __name__ == "__main__":
    sys.exit(main())
