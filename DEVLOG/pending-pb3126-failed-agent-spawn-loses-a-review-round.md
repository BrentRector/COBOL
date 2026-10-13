## PB3126 filed (a failed agent spawn loses a rolling wave's review round, and the watchdog cannot get past it)

Filed kb/Work/PB3126 from wave 1071 (run `wf_4959cab1-c5a`). Group B's round-2 review found 2 findings on
`worktree-wf_4959cab1-c5a-11`, one of them a medium correctness finding: an object data-name spelled like a METHOD-ID
now passes binding and dies in Roslyn with CS0102. Its review finisher `fix-B-r2` never started. The journal holds
`{"type": "failed", "agentId": ""}` and no transcript, and the clean worktree `wf_4959cab1-c5a-20` was left at main.
`wf_rolling_wave.js` treats that failure like a finisher that ran and returned nothing (`NO-RESULT`), so the branch
keeps its `FINDINGS 2` stamp for the next wave instead of getting one retry.

The same failure stops the watchdog. `stall_watch.py` exits on the `failed` entry, as O8 designs. A restart then
re-reports the same entry and exits again, so the rest of the wave ran unwatched and the unit fell back to `--once`
snapshots. The pattern has occurred 4 times, in 3 of this account's 37 workflow journals (2026-10-10, 10-12 and 10-13).
