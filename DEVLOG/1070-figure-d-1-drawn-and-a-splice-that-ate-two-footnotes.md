## Entry 1070 — 2026-07-28 03:40 PDT — Figure D.1 drawn, and a splice that ate two footnotes

D.1, the Format 1 SEARCH chart, is drawn: three decisions down the main column, each True exit running to its
own `imperative-statement-N` box in a second column, those three gathering into a brace carrying footnote `**`,
and the last box looping back to the entrance.

**Then I deleted content, and every gate stayed green.** The splice replaces everything between a figure's
caption and the next anchor — which for an undrawn figure is the loose text standing in for it. For D.1 that
range also held the standard's two FOOTNOTES, the prose explaining the `*` and `**` markers, printed beneath
the chart. They went, silently. Nothing caught it because a figure replacement is *expected* to change words,
so word-conservation reasoning does not apply, and the lint only asks whether a figure has a body.

Restored, and the splice now stops at the first line that reads as a SENTENCE — a figure's own labels are
fragments, so a line ending in a full stop with more than six words is the standard's prose and not part of the
figure. I wrote it as a hard failure first and watched it fire on the restored footnotes before relaxing it to
"replace only what precedes them", because a guard that has never been seen to fire is not a guard.

That is the third time this session the same lesson has landed: the checks that pass are not evidence when they
were never looking at the thing that changed.

Annex D is seven of nine. **Two remain, both structural rather than flowcharts** — D.3, compilation-group and
run-unit structure, and D.6, an example page layout. Neither is a chain of decisions, so neither generator
helps.

Gates: sweep 484/484 · publishable at 47,112 lines · 3,811 links, zero dangling. Lint red at 2.

