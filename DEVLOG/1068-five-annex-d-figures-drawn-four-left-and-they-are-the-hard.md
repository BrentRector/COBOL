## Entry 1068 — 2026-07-28 02:30 PDT — Five Annex D figures drawn; four left, and they are the hard ones

D.7, D.9, D.11, D.13 and D.14 are drawn. Two generators do it, each written against the printed page and each
tested differently.

**The truth-chart generator is tested against the one figure that was already right.** D.7, D.8 and D.9 are one
family — a chain of `Evaluate condition-N` boxes alternating with a rounded decision, each yes/no exit either
continuing down or leaving for a rail to `Truth value is true` / `is false`. D.8 was the only one drawn properly
by hand, so `--verify` requires the generator to reproduce it CHARACTER FOR CHARACTER before its output for the
other two can be trusted. It reproduces 32 of 33 rows, and the one difference is a correction: the hand-drawn
right terminal centres `Truth value` on column 57 and `is true` on 56, leaving the label askew under its own
arrowhead. Anything else differing fails.

That test earned its keep immediately. **`str.center` is not centring**: for an odd margin it puts the extra
space on the LEFT (`marg // 2 + (marg & width & 1)`), so every box came out a column off and the check failed
on every text row. I would not have noticed by looking.

**The VARYING generator is tested by construction instead**, since none of its family was drawn correctly. Every
box is sized from its content, every connector sits at a computed column, and `put` refuses to overwrite a
non-blank cell. Three defects surfaced that way, each of which would have rendered as a perfectly plausible
picture: D.11's True branch drawn straight through the decision box's right wall, D.13's loop line through the
bottom box's border, and — after moving D.9's left rail so its terminal was not clipped at column 0 — a `yes`
label landing on a box corner. A fourth was caught by making arrivals real junctions: D.13's loop arrow had
terminated in blank space, pointing at a box edge that was not on that row.

**What is left is the harder half**, and it is four figures rather than the eight I first reported:

  D.10  the truth chart with a genuine TWO-COLUMN layout — conditions 1–2 on the left, 3–4 on the right,
        with the left column continuing down as the `false` collector
  D.1   the SEARCH decision chart
  D.3   compilation-group and run-unit structure
  D.6   an example page layout

D.12 remains ragged (2 box walls in columns no rule reaches, text overflowing its border) and is the other
two-column VARYING chart. None of these fits either generator: they are not chains of boxes down one axis.

The lint is red at 4, deliberately.

Gates: sweep 484/484 · publishable at 47,101 lines · 3,811 links, zero dangling.

