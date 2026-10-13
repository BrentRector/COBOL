## Entry 2008 — 2026-10-13 15:35 PDT — Wave-1071 leads filed (PB3126-PB3133) and the registrar brief lands only through the queue

Filed kb/Work/PB3126 from wave 1071 (run `wf_4959cab1-c5a`). Group B's round-2 review found 2 findings on
`worktree-wf_4959cab1-c5a-11`, one of them a medium correctness finding: an object data-name spelled like a METHOD-ID
now passes binding and dies in Roslyn with CS0102. Its review finisher `fix-B-r2` never started. The journal holds
`{"type": "failed", "agentId": ""}` and no transcript, and the clean worktree `wf_4959cab1-c5a-20` was left at main.
`wf_rolling_wave.js` treats that failure like a finisher that ran and returned nothing (`NO-RESULT`), so the branch
keeps its `FINDINGS 2` stamp for the next wave instead of getting one retry.

The same failure stops the watchdog. `stall_watch.py` exits on the `failed` entry, as O8 designs. A restart then
re-reports the same entry and exits again, so the rest of the wave ran unwatched and the unit fell back to `--once`
snapshots. The pattern has occurred 4 times, in 3 of this account's 37 workflow journals (2026-10-10, 10-12 and 10-13).

Also filed from wave 1071's implementer reports, one note per mechanism (none duplicated an open note; PB2077, PB3120, PB546 and PB2532 are held by branches, so each lead is a new note that names its source): PB3127 (the CALL lane's ANY LENGTH arms compare category and usage only), PB3128 (a run-time slice of an ANY LENGTH / DYNAMIC LENGTH item into a typed group formal is refused), PB3129 (a non-strong group with a pointer leaf cannot cross a CALL or sit in a prototype/definition pair; repro reproduced), PB3130 (58 doc comments with two `<summary>` elements, counted on main), PB3131 (the gate plan leaves SpecTraceabilityInventoryDriftTests to leg 2 when the inventory changed), PB3132 (the section 8.3.2.2 census counts declarations only), PB3133 (OBJECT-COMPUTER's COBOL-85 clause figure is unscreened; both repros compile with exit 0).

The registrar brief (`.claude/skills/workstream/templates/registrar-brief.md`) still told a registrar to land by `git push origin HEAD:main`, a path kb/Work PB3024 deleted; it now names `enqueue.py land <branch> --lane registrar`.

