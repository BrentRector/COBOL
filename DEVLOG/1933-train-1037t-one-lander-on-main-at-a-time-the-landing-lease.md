## Entry 1933 — 2026-10-08 00:28 PDT — Train 1037t: one lander on main at a time (the landing lease, PB2537), one campaign rule for every wave (PB2522), the R2 fleet's refuter answered (PB2560), and every script self-test in every gate with one Windows share-retry rule (PB2563, PB2564)

**PB2537 — the landing lease (landed).** `push-main.sh` serialized only the push itself, so a landing whose final
rebase, gates and CI take about 50 minutes lost the race to every train that landed inside that window. On 2026-10-07
the attended R1 lander rebased and re-gated three times, lost the race to trains 1034 and 1034b, and ended SPLIT with
every gate and CI green and nothing on main. The owner, 19:30 PDT: "this is stupid. we cannot keep running r1
repeatedly for zero gain". Now a lander takes `scripts/orchestrator/landing_lease.py`'s lease before its final rebase
and gates (lander briefs step 2b), and a second lander waits for it. The lease is one file in the coord directory
(holder, reason, owning worktree, heartbeat) and dies 30 minutes after its last heartbeat or when its worktree is
gone; a dead lease is taken over and the takeover is logged. `push-main.sh` is the backstop every landing passes: it
takes or re-takes the lease, exits 4 while another worktree holds it, renews it every minute through CI, checks it
again before the main push and releases it on exit, so an unleased landing can no longer move main under a leased
one. `next_unit.py` defers `land` while a lease is live and names the holder; `stop.ps1 -Status` prints the lease
line. The coord directory's mutex moved from `alloc.py` into `coord.py#locked` (moved, not copied). The self-review
(silent-failure-hunter, six findings, all fixed) made reads take the lock too (an open reader makes `os.replace` fail
on Windows), an unreadable lease exit 2 rather than read as free, the renewer survive a transient error, a failed
release loud, and the nothing-to-land path release the lease; `push-main.sh`'s UNVERIFIED exit moved 3 → 5, because 3
is the landing check's "never retry". `LandingLeaseDriftTests` runs the nine-arm self-test, each arm seen red under a
mutation. Filed: PB2563 (eleven of the fourteen orchestrator self-tests, `test_orchestrate.ps1` among them, run in no
gate and no CI job) and PB2564 (the Windows share retry is written twice, `coord.sharing_retry` and
`gate_slot._read_shared/_replace_shared`); both land in this same train, below.

**PB2522 — one campaign rule for every wave (landed).** The campaign lane starved behind model-written handoffs.
`next_unit.py#choose` returned a handoff's `next_unit` before the campaign rule could run, so a model that wrote
`wave` in every handoff kept the lane from ever being chosen: PB2151, the legacy retirement's last note, sat ready and
undispatched from 05:40 to 14:31 PDT through eight units, three of them fix-lane waves. Rule 7 was also the only place
the lane's end was seen, so the supervisor kept logging `campaign lane: cluster PB2108` after PB2108 had landed. Now
rules 2–7 pick the unit (`#base_choice`) and one campaign rule (`#campaign_choice`) picks the lane of every wave-type
unit, whether the handoff or rule 7 chose it, keeping the handoff's reason. A handoff's `land`, `resume` or `meter`,
and an owner question, still win. `-Cluster A,B` (comma-separated, because `pwsh -File` passes `A,B` as one string)
takes several leads, which take turns, least recently run first, between fix-lane waves; a landed lead leaves the list.
Every `--cluster` choice records each lead's lane state in `units.jsonl`, and the supervisor logs `campaign <lead>
ready for N h` once a ready note has waited through more than one wave. `test_orchestrate.ps1` went from 141/158 on
the pre-change scripts to 160/160.

**PB2560 — the R2 fleet's claim refuter, answered (landed).** The owner held R2's first batch for an adversarial review
of the rebuilt fleet (2026-10-07 20:40 PDT). Its claim refuter (w1034) upheld three of the rebuild's four claims,
REFUTED the fourth, the resume, with a simulation of the real workflow, and found two blocking gaps the rebuild never
claimed to close. All three are fixed, each with the refuter's own scenario as a test that fails on the pre-fix
workflow. *Resume was per agent, and the workflow planned from agent returns.* Every agent skipped what its own
checkpoint held, while the workflow rebuilt its read set, finding ids and skeptic chunks from what agents RETURNED: on a
relaunch turn-capped finders re-read files their finisher had read, the finisher never ran again, its findings never
reached a skeptic, recomputed chunks decided one finding twice in every lens, and a finder that wrote findings and then
returned nothing orphaned them. A Workflow script has no filesystem, so the fix hands it the disk: `r2_collect.py`,
already the checkpoint format's owner, writes `launch-args.json` (`--launch`) with each pair's on-disk state, the
workflow refuses to start without it and plans every pair from it (finders only for the unread remainder, a new finder
number per agent, a finisher after any silent agent, skeptics only for findings their lens has not decided), and agents
ask `--status` before each file or finding and record through `--append`, which validates exactly as the collector
will and refuses a reused id. `test_wf_r2_review.mjs` drives the refuter's S1, S2 and sim3; the pre-fix workflow fails
ten arms, the fixed one passes 27/27. *Nothing removed duplicates:* the planted cli batch filed one mechanism twice.
`r2_collect.mechanisms` now groups upheld findings of one wave kind whose members or sites overlap, and
`file_census_notes.py --r2` files one note per mechanism with an "Also found as" line per provenance, refuses a batch
that is not COMPLETE, and adds a later batch's finding of a filed mechanism to that note. *The modern-C# dimension had
no usable evidence:* `AnalysisLevel=latest-all` raises only CA rules, so batch 1 held 284 CA warnings and no IDE or
SYSLIB rule; the analyzer tree now gets a `.globalconfig` from `r2_inputs.MODERN_RULES`, the one feature-to-rule table,
and batch 1's shards carry IDE0305, IDE0032, IDE0078 and IDE0290 at no measurable build cost. Nine of the eleven
non-blocking findings are fixed (the domain takes the root scripts and `.claude/agents/*.md`; skeptics' corrections
applied per lens; defect `spec_refs` checked by `cite.py`; a keyed mechanical-step cache; no build command in the
read-only performance input; the census path); Critical stays MAJOR and the shard sizes were already documented.
`cite.py --help` no longer crashes on a cp1252 console. `ArchReviewFleetDriftTests` pins six new dry-run arms. Batch
1's corrected launch file is `E:\COBOL-coord\scratch\r2-8be230068\batch-1\launch-args.json`. Its implementer also
found fourteen COBOL run outputs tracked at the repository root (the files conformance goldens' relative `ASSIGN`s
name); filed as PB2608.

**PB2563 + PB2564 — every script self-test in every gate, one share-retry rule (landed).** PB2537 found that eleven of
the fourteen orchestrator self-tests (the loop's unit choice, the allocator's lock, the wave planner, the budget, the
mailbox) ran in no local gate, no Unit test and no CI job, that `landing_check.py --self-test` ran only in CI (a red no
local gate could show first, PB1957), and that every gate held its own hand list (the driver's audits named five,
CI's audits job six, `linux-gate.sh` three). `scripts/self_tests.py` now DISCOVERS every self-test under `scripts/`
(a script handling `--self-test`, read from its syntax tree or a shell case arm, or a `test_*.py`/`test_*.ps1`; 60
today) and runs them in parallel, each with a private `COBOL_COORD_DIR`, no git repository-selection variables and a
private git global config holding only an identity, so a local run and a CI run see the same git; on Windows `bash`
is Git for Windows' own (from `git --exec-path`), because a bare `bash` is System32's WSL launcher. Header markers
declare a platform-only self-test (`SELF-TEST-PLATFORM`, reported NOT RUN elsewhere with its reason) and a
build-reading one (`SELF-TEST-NEEDS: build`, run by `--built`); a tracked script discovery cannot read or parse fails
discovery instead of dropping out. Every entry point runs the runner and names no self-test: the driver's `SELF-TESTS`
audit (the audits now run concurrently, before the slot) and its post-build half beside the legs; the Linux gate's
`selftests` and `selftests-built` legs (its hand-listed `hooks` leg is gone); CI's `audits`, `greenfield-unit`
(`--built`) and `windows-build-test`, the last two after their test legs. `SelfTestDiscoveryDriftTests` holds it: a
broader text net finds nothing the runner misses, no entry point names a self-test by hand, the runner's arms are
pinned by name. `test_orchestrate.ps1` (3–6 minutes alone) is eleven parallel `test_orchestrate_*.ps1` parts over one
harness. The first Linux run found two self-tests that had never run on Linux, both red there
(`fleet_active_build.py`'s drive-root arm outside its Windows guard; `test_autostart.ps1`, now windows-only). PB2564:
`scripts/sharedfile.py` is the one Windows share-retry rule (`retry`, `read_text`, `replace_text`, `remove`);
`coord.py`, `landing_lease.py` and `gate_slot.py` use it and their copies are deleted, and its `--self-test` fails on a
second `PermissionError` retry loop anywhere under `scripts/`. Probes: a planted failing `test_planted_red.py` turned
the runner RED; a hand-named `landing_check.py --self-test` planted in the workflow turned the drift test RED. Cost:
the runner takes 67–85 s on a loaded host, set by the orchestrator parts, against the old serial audit phase's 42.5 s.
Kept: the Unit tests that pin a script's arms by name; `guard-fast.sh`'s preflight. PB1957 row 44 is settled for the
self-tests; PB2431 is narrowed to the rest of CI's audits job.

**The train.** Four clusters, one gate, one CI run. PB2537 and PB2522 both edited `next_unit.py#choose`: the merge
keeps both rules, the lease deferral in `#base_choice` (rule 2 for a handoff's `land`, rule 6 for finished branches),
returned beside the choice so the campaign rule's reason keeps it. The merge review found one composition defect, fixed
in PB2522's commit: the campaign rule took any handoff `next_unit` as a named wave, so a handoff `land` deferred by the
lease would have been reported as "the handoff named wave: <the land's reason>"; a wave now counts as named only when
the handoff named a wave-type unit (check 8f). PB2560 joined at the operator's request (00:30 PDT); it shares only
`check_practices.py` with the others, and its review found nothing. PB2563+PB2564 joined at 00:45 PDT: it was cut from
PB2537's branch before PB2522, so its split of `test_orchestrate.ps1` lacked PB2522's checks. The lander ported them
(8c handoff wave, 8d round-robin, 8e starvation, 8f with the deferred-land check, and the two-lead and starved
supervisor runs) into `test_orchestrate_campaign.ps1`, and `DESIGN-orchestrator-loop` §3 keeps PB2522's campaign-rule
text, its test references re-pointed. The train was sequenced by hand behind train 1037b and the R2 fleet
(`ea20c28e3`); the rebase over R2 conflicted only in the generated `docs/DRIFT_RULES.md`, regenerated (296 drift tests
with cluster 4). The four-cluster gate went RED twice on self-tests only, every test population exact and green both
times, on a shared host near 94 % CPU: run 20261008T074216Z-964cde on `test_orchestrate_startup.ps1` "the supervisor
returned long before the 600 s the unit would have slept" (a `duration_s -lt 60` bound), and after that fix run
20261008T075440Z-cb4b3c on startup 4j (its 90 s `WaitForExit` hang guard expired) and `test_orchestrate_winddown.ps1`
4f "fleet STOP existed when the unit wound down". Each passed alone. They ran in no gate before PB2563, which put them
in the parallel SELF-TESTS phase, so PB2563 owns them and they are fixed in its commit: the duration check is bounded
by the property's own 600 s (the fake sleeps 600 s and exits 0; the kill itself is the `killed` check), and the hang
guards (the fake's two 60 s STOP-UNIT waits and 4j's 90 s) are 600 s, so they fail only on a real hang. The winddown
red was also a real race: `orchestrate.ps1` wrote STOP-UNIT before the loop's fleet stop in one `New-Item`, so a unit
acting on STOP-UNIT could look for the fleet stop before it existed; the fleet stop is now written first. Gates on the
repaired train: `=== BUILD-LOCAL GATE:
GREEN — Conformance 11,129/11,129 · Unit 32,631/32,631 · Characterization 36/36` (lander mode, one leg, SELF-TESTS GREEN, run
20261008T080440Z-ac3a14); LINUX GATE GREEN (legs selftests, unit, characterization, conformance, guard, selftests-built; at `5146df12e`,
which differs only in the Windows-only orchestrator files above, none of which a Linux leg runs); ARCH-ORACLE IDENTICAL over 7,559 cases against 868aaaf24f0b (no
compiler change, baseline not re-recorded); semgrep PASS; `PRACTICES CHECK: GREEN`. CI's first run on the train (37748130457) was RED in `windows-build-test`, step "Script self-tests on Windows": all twelve orchestrator self-tests (`test_budget.py` and the eleven parts) failed at start, because `budget.py`'s `zoneinfo` needs the `tzdata` package on Windows, which has no system time-zone database; the local hosts carry `tzdata` 2026.4 and the runner's Python 3.12 does not, a difference no local leg could show (the CI invariant). Owned by PB2563 (it first ran these self-tests on CI's Windows runner) and fixed in its commit: the job installs `tzdata==2026.4`, pinned to the local version, before the self-tests. Inventory GAP unchanged; no
diagnostic code claimed. The first `push-main.sh` run (three clusters) took the lease and then stopped at the landing
check (exit 3, main untouched) on two false stops: `selftests-gate` (this train's fourth cluster, built on its own
PB2537 branch) and the stale `wf_4f556902-4c1-1` (PB2087's CALL arm, landed as trains 1022 and 1024).
`landing_check.py#identity` bound both to unrelated dispatch-ledger notes by declared-file overlap and dated them by the
oldest; filed as PB2609. The operator removed the stale worktree and folded `selftests-gate` in, listed in
`train1037t-manifest.json`. This is the last landing sequenced by hand: its `push-main.sh` run is the first to take the
lease. The operator restarts the loop with the new `orchestrate.ps1` and `-Cluster PB2119`, because PB2108 is landed and
the old supervisor is still running its lane.

