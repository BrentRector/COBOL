## Entry 1198 — 2026-08-07 06:20 PDT — my commit cadence outran CI, and three runs read `cancelled` where nothing had failed

**Four commits landed inside one ~25-minute CI run, so three of the four runs ended `cancelled`.** The workflow
sets `concurrency: { group: workflow-ref, cancel-in-progress: true }`, which is the right setting for a busy
branch — the alternative is a queue of stale runs on superseded trees — but it means a push kills the run in
flight, and the run PAGE for PB27 and PB35 now reads `cancelled` permanently.

⚠ **`cancelled` IS NOT `failure`, AND THE HALF-REPORTED SHAPE IS WHAT MISLEADS.** Both cancelled runs show
`success` for Guard and INV-1-strong — the fast legs, which finished before the next push — and blank for
Greenfield and Windows, which did not. Read months later without this entry, that pattern looks like two legs
breaking. It is two legs never running.

⛔ **AND THE SURVIVING RUN IS CUMULATIVE COVERAGE, NOT PER-COMMIT COVERAGE.** `c1806410` contains PB27, PB35,
PB50 and PB54, so its green (all four push jobs, Linux AND Windows, `legacy-oracle` correctly `skipped` on a
push) validates every one of them — but a RED would have needed a bisect to attribute, because no intermediate
tree was independently verified. That is the actual cost, and it is why the actions-version bump earlier today
was deliberately sequenced the other way: dispatch a baseline, let it finish, THEN push the change, so its
verdict could mean only one thing.

**The discipline is on the reading, not the config.** Recorded in §0's CI section: let a run finish before
pushing again when per-commit attribution matters — a version bump, a shared-grammar change — and otherwise read
the last green as covering the batch.

