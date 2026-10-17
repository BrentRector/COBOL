## Entry 2149 — 2026-10-17 11:25 PDT — Enqueue leads filed: doubled summaries recur, report matching recurs, the landing lane gets no hold

Extended kb/Work PB3326 with wave 1106 A's lead: the same doubled `<summary>` mechanism, measured with
`E:\COBOL-coord\scratch\w1106a\stacked_summaries.py` at 49 sites in `src/` and 4 in `tests/`, the test sites named in the note.

Extended kb/Work PB3084 with a 2026-10-17 recurrence: the w1108z clerk report's prose naming PB3175's branch drew an `enqueue.py`
BLOCKED line that was repaired by hand. The report as it stands now reads only its own branch field, so the recurrence rests on
the hand-off, not a re-measurement. No code changed.

Filed kb/Work PB3328: the landing lane has no per-branch hold, so an operator's ordering steer (S2 before everything) could not be
carried out, and three conformance-adding branches started first. Fix shapes are in the note.

