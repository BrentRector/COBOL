#!/usr/bin/env python3
"""Draw the Annex D VARYING flowcharts that were never drawn — Figures D.11 and D.13.

WHAT WAS THERE. D.11 was a rough arrow sketch (`→ Condition-1 → True → Exit`, `↑___________|`) and D.13 was
nothing but the chart's labels in reading order. Their siblings D.12 and D.14 ARE drawn, which is the odd
part — whoever drew these did alternate figures and stopped.

THE HOUSE STYLE IS ALREADY SETTLED, by Figure D.8: boxes with CENTRED text, connectors meeting a border at a
`┬`/`┴`/`├` junction rather than ending in a separate arrowhead, and a branch label sitting beside its
connector. Nothing new is invented here; these two are drawn to match what is already in the file.

GEOMETRY IS COMPUTED, NEVER COUNTED. Counting characters by eye is exactly what produced the defects in the
figures that WERE drawn — D.7 has 22 box walls in columns no rule reaches, and D.12's text spills past its own
border (`│  Set identifier-2 to cur- │` inside a 21-wide box). So a box here is sized from its content, every
connector is placed at a computed column, and `put` REFUSES to overwrite a non-blank cell. That guard caught
the first draft drawing its True branch straight through the decision's right wall and its loop line through
the bottom box's border — both of which would have rendered as a plausible-looking picture.

Printed originals: D.11 on folio 1155, D.13 on folio 1157. D.13 is D.11 with the test moved AFTER the body,
which is what TEST AFTER means, so the two differ only in where the decision sits.

    python scripts/spec/repairs/annex_d_flowcharts.py            # render both to stdout
    python scripts/spec/repairs/annex_d_flowcharts.py --apply    # splice into specs/ISO_COBOL.md
"""
from __future__ import annotations

import argparse
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parents[3]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"

PAD = 1
AXIS = 30          # the column every box is centred on
LOOP = 4           # the loop-back line runs up this column


class Canvas:
    def __init__(self):
        self.cells = {}

    def put(self, r, c, s):
        for k, ch in enumerate(s):
            if ch == " ":
                continue
            cur = self.cells.get((r, c + k))
            if cur is not None and cur != ch:
                sys.exit(f"FATAL: collision at row {r} col {c + k}: {cur!r} would become {ch!r}. "
                         f"A connector is being drawn through something already there.")
            self.cells[(r, c + k)] = ch

    def box(self, top, lines, rounded=False, enter=True, leave=True, width=None, axis=None):
        """A box centred on `axis` (AXIS by default), sized to its content.

        `enter`/`leave` cut the ┬/┴ that connectors meet. `axis` exists for the two-column charts, D.10 and
        D.12, where a branch runs a second stack of boxes down its own column."""
        axis = AXIS if axis is None else axis
        inner = (width if width is not None else max(len(x) for x in lines)) + 2 * PAD
        left = axis - (inner + 2) // 2
        tl, tr, bl, br = ("╭", "╮", "╰", "╯") if rounded else ("┌", "┐", "└", "┘")
        mid = axis - left
        top_rule = ["─"] * inner
        bot_rule = ["─"] * inner
        if enter:
            top_rule[mid - 1] = "┴"
        if leave:
            bot_rule[mid - 1] = "┬"
        self.put(top, left, tl + "".join(top_rule) + tr)
        for k, text in enumerate(lines):
            self.put(top + 1 + k, left, "│" + text.center(inner) + "│")
        bottom = top + len(lines) + 1
        self.put(bottom, left, bl + "".join(bot_rule) + br)
        return Shape(top, bottom, left, left + inner + 1, top + 1 + (len(lines) - 1) // 2)

    def junction(self, r, c, ch, expect):
        """Deliberately upgrade a border glyph to a junction — `│` becomes `├` where a branch leaves a box.

        This is the ONE case where overwriting is correct, so it is a separate method: `put` must keep
        refusing, or a connector drawn through a wall by accident looks exactly like this on the page."""
        cur = self.cells.get((r, c))
        if cur != expect:
            sys.exit(f"FATAL: expected {expect!r} at row {r} col {c} to upgrade to {ch!r}, found {cur!r}")
        self.cells[(r, c)] = ch

    def vert(self, r0, r1, c):
        for r in range(r0 + 1, r1):
            self.put(r, c, "│")

    def render(self):
        rows = max(r for r, _ in self.cells) + 1
        cols = max(c for _, c in self.cells) + 1
        return [("".join(self.cells.get((r, c), " ") for c in range(cols))).rstrip() for r in range(rows)]


class Shape:
    def __init__(self, top, bottom, left, right, mid):
        self.top, self.bottom, self.left, self.right, self.mid = top, bottom, left, right, mid


# Every box in these two charts is drawn to ONE width, as the printed originals are, so the connectors between
# them are vertical rather than stepped.
W = max(len(s) for s in ("Set identifier-2 equal to", "with current BY value", "set of statements"))


def decision_exit(cv, d, label_true="True", terminal="Exit"):
    """The True branch: out of the decision's right wall to a terminal, and the False label under it."""
    cv.junction(d.mid, d.right, "├", "│")
    cv.put(d.mid, d.right + 1, "─" * 4 + f" {label_true} " + "─" * 4 + "►")
    cv.put(d.mid, d.right + 15 + len(label_true), terminal)


def loop_back(cv, frm, to_row, join_col, join_from, loop_col=None):
    """Down out of `frm`, left along the bottom, up the margin, and right into (to_row, join_col).

    The arrival is a real JUNCTION, not an arrowhead floating near the target: whatever is already at the join
    — a box's left wall, or the vertical connector between two boxes — is upgraded to `┤`, so the line is
    visibly attached. The first draft of D.13 ran its arrow to a box edge that was not on that row at all and
    left it pointing into blank space."""
    col = LOOP if loop_col is None else loop_col
    tail = frm.bottom + 2
    cv.vert(frm.bottom, tail, AXIS)
    cv.put(tail, col, "└" + "─" * (AXIS - col - 1) + "┘")
    cv.vert(to_row, tail, col)
    cv.put(to_row, col, "┌" + "─" * (join_col - col - 1))
    cv.junction(to_row, join_col, "┤", join_from)


def figure_d11():
    """TEST BEFORE, one condition: the decision is tested before the body runs."""
    cv = Canvas()
    cv.put(0, AXIS - 4, "Entrance")
    a = cv.box(3, ["Set identifier-2 equal to", "current FROM value"], enter=True, width=W)
    cv.vert(0, a.top, AXIS)
    d = cv.box(a.bottom + 3, ["Condition-1"], rounded=True, width=W)
    cv.vert(a.bottom, d.top, AXIS)
    decision_exit(cv, d)
    cv.put(d.bottom + 1, AXIS + 2, "False")
    b = cv.box(d.bottom + 3, ["Execute specified", "set of statements"], width=W)
    cv.vert(d.bottom, b.top, AXIS)
    c = cv.box(b.bottom + 3, ["Augment identifier-2", "with current BY value"], leave=True, width=W)
    cv.vert(b.bottom, c.top, AXIS)
    loop_back(cv, c, d.mid, d.left, '│')
    return cv.render()


def figure_d13():
    """TEST AFTER, one condition: the body runs first, and the loop re-enters ABOVE the body."""
    cv = Canvas()
    cv.put(0, AXIS - 4, "Entrance")
    a = cv.box(3, ["Set identifier-2 equal to", "current FROM value"], width=W)
    cv.vert(0, a.top, AXIS)
    b = cv.box(a.bottom + 4, ["Execute specified", "set of statements"], width=W)
    cv.vert(a.bottom, b.top, AXIS)
    d = cv.box(b.bottom + 3, ["Condition-1"], rounded=True, width=W)
    cv.vert(b.bottom, d.top, AXIS)
    decision_exit(cv, d)
    cv.put(d.bottom + 1, AXIS + 2, "False")
    c = cv.box(d.bottom + 3, ["Augment identifier-2", "with current BY value"], width=W)
    cv.vert(d.bottom, c.top, AXIS)
    # The loop re-enters between the first box and the body, not at the decision.
    loop_back(cv, c, a.bottom + 2, AXIS, '│')
    return cv.render()


def figure_d14():
    """TEST AFTER, two conditions.

    Printed original, folio 1158. A single main column with THREE returns: condition-2's True bypasses the
    inner augment, condition-2's False path loops back above the body, and condition-1's False path loops all
    the way back above `Set identifier-5`. The two loops run at different columns so they do not overlap."""
    cv = Canvas()
    outer, inner, bypass = 4, 10, AXIS + 20
    cv.put(0, AXIS - 4, "Entrance")
    a = cv.box(3, ["Set identifier-2 to", "current FROM value"], width=W)
    cv.vert(0, a.top, AXIS)

    b = cv.box(a.bottom + 4, ["Set identifier-5 to", "current FROM value"], width=W)
    cv.vert(a.bottom, b.top, AXIS)
    outer_join = a.bottom + 2

    c = cv.box(b.bottom + 4, ["Execute specified set", "of statements"], width=W)
    cv.vert(b.bottom, c.top, AXIS)
    inner_join = b.bottom + 2

    d2 = cv.box(c.bottom + 3, ["Condition-2"], rounded=True, width=W)
    cv.vert(c.bottom, d2.top, AXIS)
    cv.junction(d2.mid, d2.right, "├", "│")
    cv.put(d2.mid, d2.right + 1, "─" * (bypass - d2.right - 1) + "┐")
    cv.put(d2.mid - 1, d2.right + 3, "True")
    cv.put(d2.bottom + 1, AXIS + 2, "False")

    e = cv.box(d2.bottom + 3, ["Augment identifier-5", "with current BY value"], width=W)
    cv.vert(d2.bottom, e.top, AXIS)
    # condition-2 False loops back above the body; its True branch rejoins just below this box.
    loop_back(cv, e, inner_join, AXIS, "│", loop_col=inner)
    rejoin = e.bottom + 3
    cv.vert(d2.mid, rejoin, bypass)
    cv.put(rejoin, AXIS, "┌" + "─" * (bypass - AXIS - 1) + "┘")

    d1 = cv.box(rejoin + 2, ["Condition-1"], rounded=True, width=W)
    cv.vert(rejoin, d1.top, AXIS)
    decision_exit(cv, d1)
    cv.put(d1.bottom + 1, AXIS + 2, "False")

    f = cv.box(d1.bottom + 3, ["Augment identifier-2", "with current BY value"], width=W)
    cv.vert(d1.bottom, f.top, AXIS)
    loop_back(cv, f, outer_join, AXIS, "│", loop_col=outer)
    return cv.render()


def figure_d12():
    """TEST BEFORE, two conditions — the only VARYING chart with a second COLUMN.

    Printed original, folio 1156. Condition-2's True branch does not rejoin the main column: it runs a stack of
    two boxes down its own column, which then loops back to condition-1. Condition-2's False path runs the body
    and loops back to condition-2 itself. So there are two independent loops at different depths, plus a
    column of its own — none of which the single-axis charts need."""
    cv = Canvas()
    right = AXIS + 33          # clear of the main column: a box is 29 wide, so this leaves a 4-column gutter
    outer, inner = 4, 10
    cv.put(0, AXIS - 4, "Entrance")
    a = cv.box(3, ["Set identifier-2 to", "current FROM value"], width=W)
    cv.vert(0, a.top, AXIS)
    b = cv.box(a.bottom + 3, ["Set identifier-5 to", "current FROM value"], width=W)
    cv.vert(a.bottom, b.top, AXIS)

    d1 = cv.box(b.bottom + 4, ["Condition-1"], rounded=True, width=W)
    cv.vert(b.bottom, d1.top, AXIS)
    d1_join = b.bottom + 2
    decision_exit(cv, d1)
    cv.put(d1.bottom + 1, AXIS + 2, "False")

    d2 = cv.box(d1.bottom + 4, ["Condition-2"], rounded=True, width=W)
    cv.vert(d1.bottom, d2.top, AXIS)
    d2_join = d1.bottom + 2
    # True leaves for the right-hand column rather than a terminal.
    cv.junction(d2.mid, d2.right, "├", "│")
    cv.put(d2.mid, d2.right + 1, "─" * (right - d2.right - 1) + "┐")
    cv.put(d2.mid - 1, d2.right + 3, "True")
    cv.put(d2.bottom + 1, AXIS + 2, "False")

    body = cv.box(d2.bottom + 3, ["Execute specified set", "of statements"], width=W)
    cv.vert(d2.bottom, body.top, AXIS)
    aug5 = cv.box(body.bottom + 3, ["Augment identifier-5", "with current BY value"], width=W)
    cv.vert(body.bottom, aug5.top, AXIS)
    loop_back(cv, aug5, d2_join, AXIS, "│", loop_col=inner)

    # The right-hand column, fed by condition-2's True exit.
    r1 = cv.box(d2.bottom + 3, ["Augment identifier-2", "with current BY value"], width=W, axis=right)
    cv.vert(d2.mid, r1.top, right)
    r2 = cv.box(r1.bottom + 3, ["Set identifier-5 to", "current FROM value"], width=W, axis=right)
    cv.vert(r1.bottom, r2.top, right)
    # …and back to condition-1, down the outer margin.
    tail = max(aug5.bottom, r2.bottom) + 3
    cv.vert(r2.bottom, tail, right)
    cv.put(tail, outer, "└" + "─" * (right - outer - 1) + "┘")
    cv.vert(d1_join, tail, outer)
    cv.put(d1_join, outer, "┌" + "─" * (AXIS - outer - 1))
    cv.junction(d1_join, AXIS, "┤", "│")
    return cv.render()


def figure_d1():
    """Format 1 SEARCH statement having two WHEN phrases (printed folio 1033).

    Three decisions down the main column, each True exit running to its own `imperative-statement-N` box in a
    second column; those three boxes exit right to a common brace carrying footnote `**`. The last box loops
    back to the entrance. The `*` markers are the standard's own footnote references and are reproduced where
    it puts them — both footnotes are transcribed beneath the figure."""
    cv = Canvas()
    # The gap between the columns has to hold the longest branch label,  — 15 columns.
    axis, right, brace = 30, 80, 106
    loop = 2
    cv.put(0, axis - 4, "Entrance")
    join = 2
    d1 = cv.box(4, ["Index Setting: exceeds highest",
                    "permissible occurrence, or is",
                    "zero or negative"], rounded=True, axis=axis)
    cv.vert(0, d1.top, axis)

    exits = []
    i1 = cv.box(d1.mid - 1, ["imperative-statement-1"], axis=right, enter=False, leave=False)
    cv.junction(d1.mid, d1.right, "├", "│")
    cv.put(d1.mid, d1.right + 1, "─" * (i1.left - d1.right - 1))
    cv.junction(d1.mid, i1.left, "┤", "│")
    cv.put(d1.mid - 1, d1.right + 2, "True (AT END) *")
    exits.append(i1)

    cv.put(d1.bottom + 1, axis + 2, "False")
    d2 = cv.box(d1.bottom + 3, ["Condition-1"], rounded=True, axis=axis)
    cv.vert(d1.bottom, d2.top, axis)
    i2 = cv.box(d2.mid - 1, ["imperative-statement-2"], axis=right, enter=False, leave=False)
    cv.junction(d2.mid, d2.right, "├", "│")
    cv.put(d2.mid, d2.right + 1, "─" * (i2.left - d2.right - 1))
    cv.junction(d2.mid, i2.left, "┤", "│")
    cv.put(d2.mid - 1, d2.right + 2, "True")
    exits.append(i2)

    cv.put(d2.bottom + 1, axis + 2, "False")
    d3 = cv.box(d2.bottom + 3, ["Condition-2"], rounded=True, axis=axis)
    cv.vert(d2.bottom, d3.top, axis)
    cv.put(d3.top - 1, d3.right + 2, "*")
    i3 = cv.box(d3.mid - 1, ["imperative-statement-3"], axis=right, enter=False, leave=False)
    cv.junction(d3.mid, d3.right, "├", "│")
    cv.put(d3.mid, d3.right + 1, "─" * (i3.left - d3.right - 1))
    cv.junction(d3.mid, i3.left, "┤", "│")
    cv.put(d3.mid - 1, d3.right + 2, "True")
    exits.append(i3)

    # The three control transfers gather into one brace, which carries footnote `**`.
    # The rail is drawn FIRST, so each arrival can upgrade it to a junction. Drawing it afterwards means
    # running a vertical through the `┤` the middle branch already placed, which `put` refuses.
    cv.vert(exits[0].mid, exits[-1].mid, brace)
    for n, b in enumerate(exits):
        cv.junction(b.mid, b.right, "├", "│")
        cv.put(b.mid, b.right + 1, "─" * (brace - b.right - 1))
        if n == 0:
            cv.put(b.mid, brace, "┐")
        elif n == len(exits) - 1:
            cv.put(b.mid, brace, "┘")
        else:
            cv.junction(b.mid, brace, "┤", "│")
    cv.put(exits[1].mid, brace + 2, "**")

    cv.put(d3.bottom + 1, axis + 2, "False")
    a = cv.box(d3.bottom + 3, ["Increment index-name", "for identifier-1"], axis=axis)
    cv.vert(d3.bottom, a.top, axis)
    b = cv.box(a.bottom + 3, ["Increment index-name-1 (for a", "different table) or identifier-2"],
               axis=axis)
    cv.vert(a.bottom, b.top, axis)
    cv.put(b.top - 1, b.right + 2, "*")
    # …and back to the entrance.
    tail = b.bottom + 2
    cv.vert(b.bottom, tail, axis)
    cv.put(tail, loop, "└" + "─" * (axis - loop - 1) + "┘")
    cv.vert(join, tail, loop)
    cv.put(join, loop, "┌" + "─" * (axis - loop - 1))
    cv.junction(join, axis, "┤", "│")
    return cv.render()


FIGURES = {"D.1": figure_d1, "D.11": figure_d11, "D.12": figure_d12,
           "D.13": figure_d13, "D.14": figure_d14}


def splice(lines, num, body):
    """Replace whatever stands in for figure `num` with the drawn form.

    ⚠ WHAT THIS REPLACES IS EVERYTHING BETWEEN THE CAPTION AND THE NEXT ANCHOR, which for an undrawn figure is
    the loose text standing in for it — but not only that. Figure D.1 is followed by its two FOOTNOTES, prose
    the standard prints beneath the chart, and the first run of this function deleted them silently: the gates
    all stayed green because a figure's replacement is expected to change words.

    So a removed line that reads as a SENTENCE stops the run. The figure's own labels are fragments; a line
    ending in a full stop is the standard's prose and does not belong to the figure."""
    cap = next(i for i, l in enumerate(lines) if l.startswith(f"**Figure {num} — "))
    end = next(i for i in range(cap + 1, len(lines))
               if lines[i].startswith("<a id=") or lines[i].startswith("**Figure "))
    def is_prose(l):
        s = l.strip()
        return s.endswith(".") and len(s.split()) > 6 and not s.startswith(("│", "<"))

    first_prose = next((i for i in range(cap + 1, end) if is_prose(lines[i])), None)
    if first_prose is not None:
        end = first_prose                       # keep the footnotes; replace only what precedes them
    repl = ["", '<pre style="line-height:1">'] + body + ["</pre>", ""]
    lines[cap + 1:end] = repl
    return len(repl), end - cap - 1


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    drawn = {num: fn() for num, fn in FIGURES.items()}
    if not args.apply:
        for num, body in drawn.items():
            print(f"=== Figure {num} ===")
            print("\n".join(body))
            print()
        return 0

    lines = SPEC_MD.read_text(encoding="utf-8").splitlines()
    for num, body in drawn.items():
        new, old = splice(lines, num, body)
        print(f"Figure {num}: {old} lines → {new}")
    SPEC_MD.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
    print(f"\napplied to {SPEC_MD.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
