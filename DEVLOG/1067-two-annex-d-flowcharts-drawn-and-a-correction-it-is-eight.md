## Entry 1067 — 2026-07-28 01:06 PDT — Two Annex D flowcharts drawn, and a correction: it is eight figures, not five

Drew Figures D.11 and D.13, the two VARYING flowcharts that had never been drawn — D.11 was a rough arrow
sketch (`→ Condition-1 → True → Exit`, `↑___________|`) and D.13 was the chart's labels in reading order.

**The style was not a decision to make.** Figure D.8 already settles it: boxes with CENTRED text, connectors
meeting a border at a `┬`/`┴`/`├` junction rather than ending in a floating arrowhead, branch labels beside
their connector. These two are drawn to match what was already in the file.

**The geometry is computed, never counted**, because counting by eye is exactly what produced the defects in
the figures that WERE drawn: D.7 has 22 box walls in columns no rule reaches, and D.12's text spills past its
own border. So a box is sized from its content, every connector sits at a computed column, and `put` refuses
to overwrite a non-blank cell. That guard earned itself twice in ten minutes: the first draft drew D.11's True
branch straight through the decision box's right wall, and D.13's loop line through the bottom box's border.
Both would have rendered as a perfectly plausible picture.

A third catch came from the same instinct. D.13's loop arrow terminated in blank space — it pointed at a box
edge that was not on that row at all. Arrivals are real junctions now: whatever is at the join is *upgraded*
to `┤` through a method that fails if the expected glyph is not there, so a line either attaches to something
or the script stops.

**⚠ I UNDER-REPORTED THIS, and the cause is worth recording.** I told the owner five Annex D figures needed
work. It is eight. My assessment script looked up to 80 lines past a caption for a `<pre>`/``` block, and when
a figure had no body it found the NEXT figure's block and called the empty one clean — D.10 and D.14 were both
scored "clean" while being nothing but loose text. A figure's body has to START at its caption; anything
further away belongs to something else. Same shape of error as the audits that passed on a document that
rendered as a wall of italics: a checker that looks in the wrong place reports success.

The true state, now tracked by `lint_rendering.py` so it cannot drift again:

  DRAWN AND CLEAN   Figure 1, D.2, D.4, D.5 (as tables), D.8, D.11, D.13
  NOT DRAWN         D.1, D.3, D.6, D.9, D.10, D.14  — caption followed by loose text
  RAGGED            D.7 (22 walls), D.12 (2 walls, text overflowing its border)

**The lint is RED at 6, deliberately.** These figures render as a run-on jumble of words, and nothing else in
the document flags them — every word is present, so word conservation passes cleanly. A gate that stays green
over that is the failure this directory exists to prevent.

Gates: sweep 484/484 · publishable at 47,078 lines · acknowledgment verbatim · 3,811 links, zero dangling.

