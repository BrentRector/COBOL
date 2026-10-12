#!/usr/bin/env python3
"""Draw the two Annex D illustrations that are STRUCTURE rather than flow — Figures D.3 and D.6.

Neither is a flowchart, which is why neither of the other two generators covers them.

D.3 — Compilation group and run unit structures (printed folio 1044) — is a nested schematic in which the
BORDER WEIGHT is the notation: the printed figure distinguishes four entities purely by how their rectangle
is drawn, and its own Legend says so. Box-drawing has exactly those weights, so the distinction survives the
transcription:

    ┏━━┓  heavy   compilation group · run unit
    ┌──┐  light   compilation unit · source unit · runtime module
    ┌╌╌┐  dashed  source element · runtime element

What the figure SAYS, and what therefore has to survive: containment (a compilation group holds compilation
units, which hold source units, which hold source elements), the example hierarchy P-1 / P-1-1 / P-1-2 /
P-1-2-1 / P-1-2-2, and — the point of the whole diagram — that runtime modules from one compilation group need
not land in the same run unit, and a run unit need not draw from one compilation group. That last part is
carried by the `To other run unit` / `From other comp. group` arrows on the flanking units.

D.6 — Example of page layout (printed folio 1132) — is a PAGE, not a chart: one vertical rule standing for the
edge of the form, the four form positions named down its left, the six PAGE clause phrases down its right, and
the report content beside them. What stood in the transcription was a three-column Markdown table, which loses
the one thing the figure is about: VERTICAL DISTANCE. A table gives every row the same height, so the void in
the middle of the body (the standard's way of saying "and further body groups") and the void between the
logical and the physical bottom of form — the unusable tail of the physical page, which is the whole reason
PAGE LIMIT and the physical form are drawn as different lines — both collapsed to nothing.

So the rows here are placed from the MEASURED printed y of every line, at the page's own 8.7 pt pitch. The
column positions cannot be taken the same way: the printed form labels wrap ("Physical Bottom of" / "Form")
to fit a narrow column, this draws them on one line, and the columns are therefore sized from their content.
The one horizontal position that IS measured is the axis the `<…>` content lines are centred on, which the
printed page sets by tab and this reproduces as a fraction of the content column.

    python scripts/spec/repairs/annex_d_structure.py            # render both
    python scripts/spec/repairs/annex_d_structure.py --apply
"""
from __future__ import annotations

import argparse
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parents[3]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"

STYLES = {
    "heavy": ("┏", "┓", "┗", "┛", "━", "┃"),
    "light": ("┌", "┐", "└", "┘", "─", "│"),
    "dashed": ("┌", "┐", "└", "┘", "╌", "╎"),
}


class Canvas:
    def __init__(self):
        self.cells = {}

    def put(self, r, c, s):
        """⚠ REFUSES to overwrite. This class was written without the guard the other two generators have,
        and the two label bands promptly merged into `Fromtotherucomp.tgroup` — unreadable, and silent. The
        guard is not optional infrastructure; it is the only thing standing between a layout slip and a
        plausible-looking picture."""
        for k, ch in enumerate(s):
            if ch == " ":
                continue
            cur = self.cells.get((r, c + k))
            if cur is not None and cur != ch:
                sys.exit(f"FATAL: collision at row {r} col {c + k}: {cur!r} would become {ch!r}")
            self.cells[(r, c + k)] = ch

    def rect(self, r0, c0, r1, c1, style, label=None):
        """A rectangle in one of the three weights, optionally with a label cut into its top edge."""
        tl, tr, bl, br, h, v = STYLES[style]
        top = tl + h * (c1 - c0 - 1) + tr
        if label:
            at = 2
            top = top[:at] + f" {label} " + top[at + len(label) + 2:]
        self.put(r0, c0, top)
        self.put(r1, c0, bl + h * (c1 - c0 - 1) + br)
        for r in range(r0 + 1, r1):
            self.put(r, c0, v)
            self.put(r, c1, v)

    def render(self):
        rows = max(r for r, _ in self.cells) + 1
        cols = max(c for _, c in self.cells) + 1
        return [("".join(self.cells.get((r, c), " ") for c in range(cols))).rstrip() for r in range(rows)]


def figure_d3():
    cv = Canvas()
    # ── the compilation group ───────────────────────────────────────────────────────────────────────
    cv.rect(0, 0, 24, 70, "heavy")
    cv.rect(1, 2, 23, 12, "light")                       # a compilation unit, flanking
    cv.rect(1, 58, 23, 68, "light")                      # …and another
    cv.rect(1, 14, 23, 56, "light")                      # the compilation unit being expanded
    cv.rect(2, 16, 22, 54, "light", "P-1")               # its source unit
    cv.rect(3, 18, 6, 52, "dashed")                      # a source element of P-1
    cv.rect(7, 18, 21, 32, "light", "P-1-1")
    cv.rect(8, 20, 20, 30, "dashed")
    cv.rect(7, 34, 21, 52, "light", "P-1-2")
    cv.rect(8, 36, 11, 50, "dashed")
    # P-1-2 holds two further source units; their names are printed above them, inside P-1-2.
    cv.put(12, 36, "P-1-2-1")
    cv.rect(13, 36, 20, 42, "light")
    cv.put(12, 44, "P-1-2-2")
    cv.rect(13, 44, 20, 50, "light")
    # …each holding a source element of its own, as the printed figure shows.
    cv.rect(14, 38, 19, 40, "dashed")
    cv.rect(14, 46, 19, 48, "dashed")

    # ── how modules reach run units ─────────────────────────────────────────────────────────────────
    for c, text in ((7, "To other run unit"), (63, "To other run unit")):
        cv.put(25, c, "│")
        cv.put(26, c, "▼")
        cv.put(27, max(0, c - len(text) // 2), text)
    cv.put(25, 35, "│")
    cv.put(26, 35, "▼")
    cv.put(27, 35, "│")

    # ── the run unit ────────────────────────────────────────────────────────────────────────────────
    # The two label bands need rows of their own: the first draft put both on row 27 and they overwrote
    # each other into nonsense.
    base = 32
    for c, text in ((7, "From other comp. group"), (63, "From other comp. group")):
        cv.put(base - 3, max(0, c - len(text) // 2), text)
        cv.put(base - 2, c, "│")
        cv.put(base - 1, c, "▼")
    cv.put(base - 2, 35, "│")
    cv.put(base - 1, 35, "▼")
    cv.rect(base, 0, base + 20, 70, "heavy")
    cv.rect(base + 1, 2, base + 19, 12, "light")         # a runtime module
    cv.rect(base + 1, 58, base + 19, 68, "light")
    cv.rect(base + 1, 14, base + 19, 56, "light")
    cv.rect(base + 3, 18, base + 6, 52, "dashed")        # runtime elements, mirroring the source elements
    cv.rect(base + 8, 18, base + 18, 32, "dashed")
    cv.rect(base + 8, 34, base + 11, 52, "dashed")
    cv.rect(base + 13, 36, base + 18, 42, "dashed")
    cv.rect(base + 13, 44, base + 18, 50, "dashed")

    # ── the legend, which is where the notation is stated ───────────────────────────────────────────
    L = 74
    cv.put(0, L + 6, "Legend")
    rows = [("heavy", "Compilation Group"), ("light", "Compilation Unit"),
            ("light", "Source Unit"), ("dashed", "Source Element"),
            ("heavy", "Run Unit"), ("light", "Runtime Module"), ("dashed", "Runtime Element")]
    r = 2
    for style, name in rows:
        cv.rect(r, L, r + 2, L + 21, style)
        cv.put(r + 1, L + 2, name)
        r += 4
    return cv.render()


# ── Figure D.6, measured off printed folio 1132 ─────────────────────────────────────────────────────
# Every line of the printed figure, with the y at which the standard sets it. ROW() turns those into text
# rows at the page's own pitch, which is what preserves the two gaps that MEAN something — the one in the
# middle of the body, and the one above the last line the physical page can print.
Y0, PITCH = 103.9, 8.7                    # y of the first content line · the page's line pitch, in points
RULE_BOTTOM = 454.6                       # the printed rule runs on past the last row, closing the form
CONTENT_LEFT = 236.5                      # x where every content SENTENCE starts
PLACEHOLDER_AXIS = 325.4                  # mean x-centre of the ten `<…>` lines — the page sets these by tab
PT_PER_CHAR = 3.50                        # from the content font: 70 chars spanning 236.5 → 480.9

SENTENCE, PLACEHOLDER = "sentence", "placeholder"

# A form-position label is attached to the SENTENCE it names, not to its own printed y: the printed labels
# wrap over two or three lines and only one of the four happens to start on its sentence's row.
D6 = [
    (103.9, "Physical Top of Form",    None,                   SENTENCE,    "the first line that can be printed"),
    (119.7, None,                      None,                   PLACEHOLDER, "<blank>"),
    (136.5, "Logical Top of Form",     "Heading",              SENTENCE,    "the first line of the report or page heading"),
    (145.2, None,                      None,                   PLACEHOLDER, "<(first page only) report header lines>"),
    (154.0, None,                      None,                   PLACEHOLDER, "<page heading lines>"),
    (165.1, None,                      "First Detail",         SENTENCE,    "the first line of a body group"),
    (173.9, None,                      None,                   PLACEHOLDER, "<Control Heading lines>"),
    (182.7, None,                      None,                   PLACEHOLDER, "<Detail lines>"),
    (191.4, None,                      None,                   PLACEHOLDER, "<Control Footing lines>"),
    (198.5, None,                      None,                   PLACEHOLDER, "<Control Heading lines>"),
    (207.2, None,                      None,                   PLACEHOLDER, "<Detail lines>"),
    (216.0, None,                      None,                   PLACEHOLDER, "<Control Footing lines>"),
    (267.8, None,                      None,                   PLACEHOLDER, "<Control Heading lines>"),
    (276.5, None,                      None,                   PLACEHOLDER, "<Detail lines>"),
    (285.3, None,                      None,                   PLACEHOLDER, "<Control Footing lines>"),
    (294.0, None,                      "Last Control Heading", SENTENCE,    "the last line on which a control heading may print"),
    (302.8, None,                      None,                   PLACEHOLDER, "<Detail lines>"),
    (322.7, None,                      "Last Detail",          SENTENCE,    "the last line on which Detail lines will print"),
    (331.5, None,                      None,                   PLACEHOLDER, "<Control Footing lines>"),
    (349.0, None,                      "Footing",              SENTENCE,    "last line of Control footing or first line of a report or page footing"),
    # `<page footing lines.` is printed exactly so — the standard's own unclosed bracket, kept verbatim.
    (357.7, None,                      None,                   PLACEHOLDER, "<page footing lines."),
    (366.5, None,                      None,                   PLACEHOLDER, "<(last page only) report footing lines>"),
    (384.0, "Logical Bottom of Form",  "Page Limit",           SENTENCE,    "the last line on a logical page"),
    (436.5, "Physical Bottom of Form", None,                   SENTENCE,    "the last line that can be printed"),
]


def figure_d6():
    row = lambda y: round((y - Y0) / PITCH)                                              # noqa: E731
    form_w = max(len(r[1]) for r in D6 if r[1])
    phrase_w = max(len(r[2]) for r in D6 if r[2])
    rule_col = form_w + 1                                # the bar is kept one space clear, per FIGURE-STYLE
    phrase_col = rule_col + 2
    content_col = phrase_col + phrase_w + 2
    axis = content_col + round((PLACEHOLDER_AXIS - CONTENT_LEFT) / PT_PER_CHAR)
    head = 2                                             # the column header, then a blank row

    cv = Canvas()
    # The header sits above the figure on the PRECEDING printed page, under the caption. It labels the two
    # right-hand columns; the form positions are unlabelled there too.
    cv.put(0, phrase_col, "PAGE clause phrases")
    cv.put(0, content_col, "Report content")
    for y, form, phrase, kind, text in D6:
        r = head + row(y)
        if form:
            cv.put(r, 0, form)
        if phrase:
            cv.put(r, phrase_col, phrase)
        # A sentence starts at the column; a `<…>` line is centred on the measured axis, with the odd
        # column falling to its left — the convention `centre()` sets in the other two generators.
        cv.put(r, content_col if kind == SENTENCE else axis - len(text) // 2, text)
    for r in range(head, head + row(RULE_BOTTOM) + 1):
        cv.put(r, rule_col, "│")
    return cv.render()


def escape(body):
    """`<blank>` inside a raw `<pre>` is an HTML TAG, and an unknown tag is DROPPED by every sanitizing
    renderer — the words would vanish off the page while every text-level audit stayed green, which is the
    exact silent-rendering class `lint_rendering.py` exists to catch. The canvas holds the real characters,
    so geometry and the collision guard are computed on what the standard prints; the entities go on at
    write time. No-op for D.3, which has no angle bracket in it."""
    return [l.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;") for l in body]


def splice(lines, num, body):
    """Replace whatever stands in for figure `num` with the drawn form.

    ⚠ This replaces EVERYTHING between the caption and the next anchor — which for D.1 also held the
    standard's two footnotes, deleted silently on the first run. A removed line that reads as a SENTENCE
    therefore stops the run: a figure's own labels are fragments, and a line ending in a full stop is the
    standard's prose. (D.6's content lines end in no full stop, and its `<…>` lines are excluded outright.)"""
    cap = next(i for i, l in enumerate(lines) if l.startswith(f"**Figure {num} — "))
    end = next(i for i in range(cap + 1, len(lines))
               if lines[i].startswith("<a id=") or lines[i].startswith("**Figure "))

    def is_prose(l):
        s = l.strip()
        return (s.endswith(".") and len(s.split()) > 6
                and not s.startswith(("│", "<", "┃", "|", "&lt;")))

    first = next((i for i in range(cap + 1, end) if is_prose(lines[i])), None)
    if first is not None:
        end = first
    lines[cap + 1:end] = ["", '<pre style="line-height:1">'] + escape(body) + ["</pre>", ""]


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    figures = [("D.3", figure_d3()), ("D.6", figure_d6())]
    if not args.apply:
        for num, body in figures:
            print(f"── Figure {num} " + "─" * 60)
            print("\n".join(body))
            print()
        return 0

    lines = SPEC_MD.read_text(encoding="utf-8").splitlines()
    for num, body in figures:
        splice(lines, num, body)
        print(f"Figure {num}: drawn ({len(body)} rows)")
    SPEC_MD.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
    print(f"applied to {SPEC_MD.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
