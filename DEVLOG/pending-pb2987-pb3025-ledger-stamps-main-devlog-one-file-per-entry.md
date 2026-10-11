## PB2987 + PB3025: the ledger's owed check reads main, and the DEVLOG becomes one file per entry

**PB2987 — the owed-publish check read a stale checkout.** After train 1056 landed the supervisor announced no ledger
publish: `ledger_state.py owed --repo $RepoDir` stamped the main checkout's HEAD, and every landing is pushed from a
lander's own worktree, so that checkout stays at whatever train someone last fast-forwarded it to. The stamp is now
read on `origin/main`, fetched first (`--main-ref`); the checkout's own stamp is reported beside it, because the page
is still rendered from the checkout: `owed` and `gen_ledger.py` name a stale checkout and the fast-forward that cures
it, and `mark-published` refuses while the checkout is not main's, so a page that does not describe main is never
marked current. `test_ledger_state.py` now runs against a bare origin and pushes a landing from a second clone. The
other `--repo $RepoDir` readers read branches, worktrees and the coordination directory, not the tree; `next_unit.py`'s
campaign lane reads `kb/Work` from the scripts' own checkout and is stale the same way (reported, not fixed here).

**PB3025 — one file per DEVLOG entry (owner 2026-10-11, PB3015 Q2).** With a merge queue, two landings that both
insert at the top of one `DEVLOG.md` collide. `scripts/devlog.py` is now the one tool: `render` (the README, then the
entries newest first, concatenated byte for byte), `stamp <branch>` (the implementer's unnumbered
`DEVLOG/pending-<slug>.md` gets its number from `alloc.py devlog` and its header stamp, in one commit), `check` (the
landing rule `landing_devlog.py` was: exactly one new stamped entry numbered above main's, and nothing malformed,
pending, duplicated, deleted or monolithic), `next`, and a 33-arm self-test that also lints the repository's own log.
`push-main.sh` exit 6 and a new CI `audits` step run `check`; `alloc.py`, `plan_wave.py`, `inventory_ratchet.py`,
`census.py` and `check_practices.py` read `DEVLOG/`; `plan_wave.py` plans no DEVLOG number any more.
`landing_devlog.py` and `prepend-devlog.py` are deleted.

The migration split the committed 7,169,694-byte `DEVLOG.md` (2,952 CRLF and 69,448 LF line ends) into a README and
1,980 entry files, LF. Rendering the files reproduces the LF-normalized monolith byte for byte (sha256 c7ee9a9f…).
Every header was kept as written. The history held four surprises: twenty numbers used twice (008–014, 171–185),
entries 1191a–1191d, twelve headers with a double-encoded dash, and undated early entries. A letter in the FILE name
(`0185b-…` above `0185a-…`) keeps each duplicate pair in its original order without a hand list. CLAUDE.md rule 6, the
briefs (the implementer writes the pending entry; a train lander folds its clusters' pending files into one and
stamps it), the skills, DOC_INDEX, the plan and the two design docs describe the new shape.
