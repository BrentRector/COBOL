#!/usr/bin/env python3
"""Draw the Annex D condition-evaluation flowcharts — Figures D.7, D.8, D.9, D.10.

THE FAMILY. Each is a chain of `Evaluate condition-N` boxes alternating with a rounded decision, where each
decision's yes/no exits either continue down the chain or leave for a RAIL that runs to one of the two
terminals, `Truth value is true` / `Truth value is false`. Only the decision wording and where each exit goes
differ between them, so one generator draws all three.

  D.7  condition-1 AND condition-2 AND … condition-n     decisions ask `false`; any yes ⇒ false
  D.8  condition-1 OR  condition-2 OR  … condition-n     decisions ask `true`;  any yes ⇒ true
  D.9  condition-1 OR condition-2 AND condition-3        mixed, and the only one using BOTH rails
  D.10 (condition-1 OR NOT condition-2) AND condition-3 AND condition-4   two COLUMNS, drawn separately

D.10 is the exception the shared `chart()` does not cover: the parenthesised OR is decided in the left column
and hands over to a SECOND column of ANDed conditions, whose fallbacks return to the left column's spine. The
columns are not independent, so it has its own function.

WHY IT IS SAFE TO REGENERATE D.8, WHICH WAS ALREADY CORRECT. It is the test. D.8 is the one member of the
family that was drawn properly by hand, so the generator has to reproduce it CHARACTER FOR CHARACTER before
its output for the other two can be trusted — `--verify` asserts exactly that against the file. Without it,
this script would be an untested tool applied to figures nobody has checked.

D.7 was the ragged one: `│Condition-1│` is eleven characters inside a nine-wide box, its rail sits in a column
no rule reaches (22 such walls), and its arrows are `→` rather than box-drawing. D.9 was never drawn at all.

    python scripts/spec/repairs/annex_d_truth_charts.py --verify   # generator reproduces the printed D.8
    python scripts/spec/repairs/annex_d_truth_charts.py            # render all four
    python scripts/spec/repairs/annex_d_truth_charts.py --apply
"""
from __future__ import annotations

import argparse
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parents[3]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"

# Taken from the hand-drawn D.8, which the generator must reproduce exactly.
INNER = 21                 # width inside a box's walls
LEFT = 12                  # column of a box's left wall
AXIS = LEFT + INNER // 2 + 1
RAIL_R = 57                # the right-hand rail
RAIL_L = 6                 # the left-hand rail, used only by D.9. Far enough from the margin that
                           # its terminal label centres on it instead of being clipped at column 0.


def centre(text, width):
    """Centre with the ODD space on the right, which is what the hand-drawn D.8 does.

    `str.center` is not this function: for an odd margin it puts the extra space on the LEFT (its rule is
    `marg // 2 + (marg & width & 1)`), so every box came out one column off and the `--verify` check against
    D.8 failed on every text row."""
    pad = width - len(text)
    return " " * (pad // 2) + text + " " * (pad - pad // 2)


class Canvas:
    def __init__(self):
        self.cells = {}

    def put(self, r, c, s):
        for k, ch in enumerate(s):
            if ch == " ":
                continue
            cur = self.cells.get((r, c + k))
            if cur is not None and cur != ch:
                sys.exit(f"FATAL: collision at row {r} col {c + k}: {cur!r} would become {ch!r}")
            self.cells[(r, c + k)] = ch

    def junction(self, r, c, ch, expect):
        """The one legitimate overwrite: a border glyph becoming a junction where a branch attaches."""
        cur = self.cells.get((r, c))
        if cur != expect:
            sys.exit(f"FATAL: expected {expect!r} at row {r} col {c}, found {cur!r}")
        self.cells[(r, c)] = ch

    def box(self, top, lines, rounded=False, enter=True, leave=True, axis=None):
        """A box centred on `axis` (AXIS by default). `axis` exists for D.10, whose right-hand conditions run
        down a column of their own rather than continuing the main chain."""
        axis = AXIS if axis is None else axis
        left = axis - (INNER // 2) - 1
        tl, tr, bl, br = ("╭", "╮", "╰", "╯") if rounded else ("┌", "┐", "└", "┘")
        mid = axis - left
        rule = lambda ch: "".join(ch if i == mid - 1 else "─" for i in range(INNER))
        self.put(top, left, tl + (rule("┴") if enter else "─" * INNER) + tr)
        for k, text in enumerate(lines):
            self.put(top + 1 + k, left, "│" + centre(text, INNER) + "│")
        bottom = top + len(lines) + 1
        self.put(bottom, left, bl + (rule("┬") if leave else "─" * INNER) + br)
        return top, bottom

    def vert(self, r0, r1, c):
        for r in range(r0 + 1, r1):
            self.put(r, c, "│")

    def spine(self, r0, r1, c):
        """A vertical run that PASSES THROUGH junctions already placed on it.

        D.10's left column is drawn last but is joined part-way down by the right column's fallbacks, so its
        `├` glyphs are already there. `vert` would refuse them — correctly, since it exists to catch a line
        drawn through something — so passing through is a separate, explicit operation."""
        for r in range(r0 + 1, r1):
            if (r, c) not in self.cells:
                self.cells[(r, c)] = "│"

    def render(self):
        rows = max(r for r, _ in self.cells) + 1
        cols = max(c for _, c in self.cells) + 1
        return [("".join(self.cells.get((r, c), " ") for c in range(cols))).rstrip() for r in range(rows)]


def chart(steps, down_terminal, right_terminal, left_terminal=None):
    """steps: (condition-name, decision-word, yes-target, no-target); target ∈ right | left | down."""
    cv = Canvas()
    row = 0
    rail_r_from = rail_l_from = None
    for n, (cond, word, yes, no) in enumerate(steps):
        _, ebot = cv.box(row, ["Evaluate", cond], enter=(n > 0))
        dtop, dbot = cv.box(ebot + 2, [f"{cond[0].upper() + cond[1:]}", word], rounded=True)
        cv.vert(ebot, dtop, AXIS)
        mid = dtop + 1                                   # the decision's first text row
        if yes == "right":
            cv.junction(mid, LEFT + INNER + 1, "├", "│")
            cv.put(mid, LEFT + INNER + 2, "─" * (RAIL_R - LEFT - INNER - 2))
            cv.put(mid - 1, LEFT + INNER + 6, "yes")
            cv.put(mid, RAIL_R, "┐" if rail_r_from is None else "┤")
            if rail_r_from is not None:
                cv.vert(rail_r_from, mid, RAIL_R)
            rail_r_from = mid
        elif yes == "left":
            cv.junction(mid, LEFT, "┤", "│")
            cv.put(mid, RAIL_L + 1, "─" * (LEFT - RAIL_L - 1))
            # Sits in the gap between the rail and the box, never over either.
            cv.put(mid - 1, LEFT - 4, "yes")
            cv.put(mid, RAIL_L, "┌" if rail_l_from is None else "├")
            if rail_l_from is not None:
                cv.vert(rail_l_from, mid, RAIL_L)
            rail_l_from = mid
        if no == "down":
            cv.vert(dbot, dbot + 2, AXIS)          # the connector the `no` label sits beside
            cv.put(dbot + 1, AXIS + 2, "no")
            row = dbot + 2
        elif no == "right":
            cv.put(dbot + 1, AXIS + 2, "no")
            cv.vert(dbot, dbot + 2, AXIS)
            cv.put(dbot + 2, AXIS, "└" + "─" * (RAIL_R - AXIS - 1) + "┤")
            cv.vert(rail_r_from, dbot + 2, RAIL_R)
            rail_r_from = dbot + 2
            row = dbot + 3
    # The last `no` row IS the row the arrowhead sits on; an extra blank row here made the chart a row
    # taller than the hand-drawn D.8 and broke the character-for-character check.
    end = row
    if down_terminal:
        cv.vert(row - 2, end, AXIS)
        cv.put(end, AXIS, "▼")
        for k, line in enumerate(down_terminal):
            cv.put(end + 1 + k, AXIS - len(line) // 2, line)
    if rail_r_from is not None:
        cv.vert(rail_r_from, end, RAIL_R)
        cv.put(end, RAIL_R, "▼")
        for k, line in enumerate(right_terminal):
            cv.put(end + 1 + k, RAIL_R - len(line) // 2, line)
    if rail_l_from is not None and left_terminal:
        cv.vert(rail_l_from, end, RAIL_L)
        cv.put(end, RAIL_L, "▼")
        for k, line in enumerate(left_terminal):
            cv.put(end + 1 + k, max(0, RAIL_L - len(line) // 2), line)
    return cv.render()


TRUE = ["Truth value", "is true"]
FALSE = ["Truth value", "is false"]


def d7():
    steps = [(f"condition-{n}", "false", "right", "down") for n in ("1", "2", "n")]
    return chart(steps, TRUE, FALSE)


def d8():
    steps = [(f"condition-{n}", "true", "right", "down") for n in ("1", "2", "n")]
    return chart(steps, FALSE, TRUE)


def d9():
    steps = [("condition-1", "true", "right", "down"),
             ("condition-2", "false", "left", "down"),
             ("condition-3", "false", "left", "right")]
    return chart(steps, None, TRUE, FALSE)


def d10():
    """(condition-1 OR NOT condition-2) AND condition-3 AND condition-4 — the two-COLUMN chart.

    Printed original, folio 1149. The parenthesised OR is decided in the left column; either way of satisfying
    it hands over to a SECOND column where the two ANDed conditions are tested. The left column's spine
    continues below the hand-over as the `false` collector that the right column's yes exits fall back to, so
    the two columns are not independent — which is why this one does not fit the single-chain `chart()`.
    """
    cv = Canvas()
    right = AXIS + 38
    _, e1b = cv.box(0, ["Evaluate", "condition-1"], enter=False)
    d1t, d1b = cv.box(e1b + 2, ["Condition-1", "true"], rounded=True)
    cv.vert(e1b, d1t, AXIS)
    # Both ways of satisfying the OR leave to the right-hand column.
    cv.junction(d1t + 1, AXIS + INNER // 2 + 1, "├", "│")
    cv.put(d1t + 1, AXIS + INNER // 2 + 2, "─" * (right - AXIS - INNER // 2 - 2) + "┐")
    cv.put(d1t, AXIS + INNER // 2 + 6, "yes")
    cv.vert(d1b, d1b + 2, AXIS)
    cv.put(d1b + 1, AXIS + 2, "no")

    _, e2b = cv.box(d1b + 2, ["Evaluate", "condition-2"])
    d2t, d2b = cv.box(e2b + 2, ["Condition-2", "false"], rounded=True)
    cv.vert(e2b, d2t, AXIS)
    cv.junction(d2t + 1, AXIS + INNER // 2 + 1, "├", "│")
    cv.put(d2t + 1, AXIS + INNER // 2 + 2, "─" * (right - AXIS - INNER // 2 - 2) + "┤")
    cv.put(d2t, AXIS + INNER // 2 + 6, "yes")
    cv.vert(d1t + 1, d2t + 1, right)
    cv.put(d2b + 1, AXIS + 2, "no")

    # The right-hand column: the two ANDed conditions, each falling back left when it is not satisfied.
    _, e3b = cv.box(d2b + 2, ["Evaluate", "condition-3"], axis=right)
    cv.vert(d2t + 1, d2b + 2, right)
    d3t, d3b = cv.box(e3b + 2, ["Condition-3", "false"], rounded=True, axis=right)
    cv.vert(e3b, d3t, right)
    cv.junction(d3t + 1, right - INNER // 2 - 1, "┤", "│")
    cv.put(d3t + 1, AXIS + 1, "─" * (right - INNER // 2 - AXIS - 2))
    cv.put(d3t + 1, AXIS, "├")
    cv.put(d3t, AXIS + 6, "yes")
    cv.vert(d3b, d3b + 2, right)
    cv.put(d3b + 1, right + 2, "no")

    _, e4b = cv.box(d3b + 2, ["Evaluate", "condition-4"], axis=right)
    d4t, d4b = cv.box(e4b + 2, ["Condition-4", "false"], rounded=True, axis=right)
    cv.vert(e4b, d4t, right)
    cv.junction(d4t + 1, right - INNER // 2 - 1, "┤", "│")
    cv.put(d4t + 1, AXIS + 1, "─" * (right - INNER // 2 - AXIS - 2))
    cv.put(d4t + 1, AXIS, "├")
    cv.put(d4t, AXIS + 6, "yes")
    cv.put(d4b + 1, right + 2, "no")

    # The left spine runs the whole way down as the `false` collector.
    end = d4b + 2
    cv.spine(d2b, end, AXIS)
    cv.put(end, AXIS, "▼")
    for k, line in enumerate(FALSE):
        cv.put(end + 1 + k, AXIS - len(line) // 2, line)
    cv.vert(d4b, end, right)
    cv.put(end, right, "▼")
    for k, line in enumerate(TRUE):
        cv.put(end + 1 + k, right - len(line) // 2, line)
    return cv.render()


FIGURES = {"D.7": d7, "D.8": d8, "D.9": d9, "D.10": d10}


def body_of(lines, num):
    cap = next(i for i, l in enumerate(lines) if l.startswith(f"**Figure {num} — "))
    start = next((i for i in range(cap, cap + 12) if lines[i].strip().startswith("<pre")), None)
    if start is None:
        return cap, None, None
    end = next(i for i in range(start, len(lines)) if lines[i].strip() == "</pre>")
    return cap, start, end


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--verify", action="store_true", help="assert the generator reproduces the printed D.8")
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    lines = SPEC_MD.read_text(encoding="utf-8").splitlines()
    if args.verify:
        _, s, e = body_of(lines, "D.8")
        have = [l.rstrip() for l in lines[s + 1:e]]
        want = d8()
        diff = [k for k in range(max(len(have), len(want)))
                if (have[k] if k < len(have) else None) != (want[k] if k < len(want) else None)]
        # ONE KNOWN CORRECTION. In the hand-drawn D.8 the right terminal's two lines are centred on DIFFERENT
        # columns — `Truth value` on 57, `is true` on 56 — so the second line sits visibly askew under its own
        # arrowhead. The generator centres both on the rail, which changes that one row. Everything else has
        # to match character for character, and this fails if anything else does.
        known = {len(want) - 1}
        unexpected = [k for k in diff if k not in known]
        for k in diff:
            h = have[k] if k < len(have) else "<missing>"
            w = want[k] if k < len(want) else "<missing>"
            tag = "CORRECTED" if k in known else "UNEXPECTED"
            print(f"   row {k} [{tag}]")
            print(f"     file: {h!r}")
            print(f"     gen : {w!r}")
        if unexpected:
            print(f"\n✗ {len(unexpected)} unexpected difference(s) — the generator does not match D.8")
            return 1
        if diff:
            print(f"\n✓ reproduces the HAND-DRAWN D.8 in {len(want) - len(diff)} of {len(want)} rows. The one "
                  f"change is the right terminal's second line, which the original left a column off from its "
                  f"own first line.")
        else:
            # ⚠ Once `--apply` has run, D.8 in the file IS this generator's output, so this compares the
            # generator with itself: a DRIFT check, not a validation. The validation that mattered happened
            # once, against the hand-drawn figure, and is recorded in DEVLOG entry 1068 and the commit that
            # landed it — 32 of 33 rows, the difference being the askew terminal this corrects. Recovering
            # the original test means diffing against a revision before that commit.
            print(f"\n✓ D.8 in the file matches this generator exactly ({len(want)} rows) — a DRIFT check. "
                  f"The generator was validated against the HAND-DRAWN D.8 before it was applied; that "
                  f"comparison is only reproducible against a revision predating the apply.")
        return 0

    drawn = {n: fn() for n, fn in FIGURES.items()}
    if not args.apply:
        for n, body in drawn.items():
            print(f"=== Figure {n} ===")
            print("\n".join(body))
            print()
        return 0

    # Bottom-up so earlier indices stay valid. Derived from where each caption actually IS, rather than a
    # hardcoded list — the hardcoded one silently skipped D.10 when it was added.
    order = sorted(FIGURES, key=lambda n: next(
        i for i, l in enumerate(lines) if l.startswith(f"**Figure {n} — ")), reverse=True)
    for num in order:
        cap, s, e = body_of(lines, num)
        repl = ["", '<pre style="line-height:1">'] + drawn[num] + ["</pre>", ""]
        end = e + 1 if s is not None else next(
            i for i in range(cap + 1, len(lines)) if lines[i].startswith("<a id="))
        lines[cap + 1:end] = repl
        print(f"Figure {num}: redrawn ({len(drawn[num])} rows)")
    SPEC_MD.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
    print(f"\napplied to {SPEC_MD.relative_to(REPO)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
