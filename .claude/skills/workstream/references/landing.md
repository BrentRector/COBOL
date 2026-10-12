# Workstream landing order and mechanics (SKILL.md §3, full text)

## 3. Landing order and mechanics

- ⛔ **THE LANDING IS A SCRIPT: `python scripts/orchestrator/enqueue.py land <branch>` (kb/Work PB3023; MANDATORY-PRACTICES
  L1; DESIGN-test-build-ci.md §3.17.2 step 3).** One finished branch, one pull request. It merges origin/main into the
  branch (never a rebase: a branch that merged a same-file predecessor keeps its conflict resolutions), runs the ordered
  gate and CI's Linux legs on the merged head under the shared gate slot (a commit gated GREEN is not gated again),
  stamps the branch's `DEVLOG/pending-<slug>.md` (`devlog.py stamp`, the number from `alloc.py devlog`), runs the landing
  checks (`devlog.py check` 6, `landing_oracle.py` 7, `landing_check.py` 3) and hands the branch to GitHub's merge
  queue (`enqueue.py queue`: the PR titled from the DEVLOG entry, its body the entry and the commits; the queue's merge
  commit keeps the branch's commits ancestors of main). While `ruleset.py check` does not report the queue ACTIVE it
  lands through `push-main.sh` instead. Every run ends on one `=== ENQUEUE: ` line: run it in the background and block
  on that line. Exits: 4 conflict · 5 a gate not GREEN · 3/6/7 a landing check · 9 GitHub refused · 10 push-main did not
  land it. ⓜ 2026-10-10 under the trains: 233 of 567 lander minutes (41 %) were local whole-population gates, 94 more
  push-main holding the lease while CI ran the same population, 10 landers for 6 trains, the first commit on main 6 h
  49 min into the day (kb/Work PB3015).
- **Who runs it.** While the orchestrator loop runs (`python scripts/orchestrator/coord.py loop-state` prints `LOOP
  STATE: running`), its `enqueue` unit (`scripts/orchestrator/units/enqueue.md`) runs it for every branch `python
  scripts/prune_worktrees.py --brief` lists WAITING TO LAND, in the order it lists them (the most harmful lead first,
  kb/Work PB2981's ranking): the attended session lands nothing itself and writes each finished attended branch's report
  where the unit collects it — the file name EXACTLY `<coord>\scratch\reports\w<wave><letter>-PB<lead>-report.md`
  (plan_wave.py `REPORT_NAME`), its header:

  ```
  Status: DONE
  Worktree: E:\COBOL\.claude\worktrees\<name>
  Branch: <branch>
  HEAD: <sha>
  Base: <sha>
  ```

  `prune_worktrees.py --brief` lists it WAITING FOR REVIEW until ONE read-only `cobol-reviewer` stamps it (the `review`
  skill's branch form, MANDATORY-PRACTICES I10, kb/Work PB3027); a `Review: CLEAN` stamp at its tip makes it WAITING TO
  LAND. With the loop STOPPED the attended session runs `enqueue.py land` by hand for its own branch, no agent needed.
- **What a script cannot decide goes to ONE finisher (L12).** A conflict (exit 4), a red gate (5), a DEVLOG or oracle
  refusal (6, 7), a push-main CI red (10), or a pull request the queue BOUNCED (`prune_worktrees.py` reads ENQUEUED and
  BOUNCED from one GraphQL read of the open pull requests; offline the states are git-only) is the branch's own: one
  `cobol-finisher` with `templates/finisher-brief.md` fixes it at the root, gates, and lands it with `enqueue.py land`.
  Exit 3 (the landing check's STOP) is never retried as is. A permission refusal of `enqueue.py land` is handed off,
  never retried or routed around: `READY-TO-LAND <worktree> <branch>` (L13).
- **The landing lease lives on the push-main path only (kb/Work PB2537).** `push-main.sh` takes
  `scripts/orchestrator/landing_lease.py`'s lease, renews it while CI runs and releases it on exit (exit 4 while another
  worktree holds it), and `next_unit.py` starts no `enqueue` unit while a lease is live; so while the queue is not
  ACTIVE, landings go one at a time. A merge-queue landing takes no lease: the queue orders main. The lease, push-main.sh
  and the `ci/**` branches go with the first change landed through the queue (kb/Work PB3024).
- ⛔ **No branch is left undecided (MANDATORY-PRACTICES O10, owner 2026-10-08, kb/Work PB2600).** After every landing,
  `python scripts/prune_worktrees.py` (the lifecycle survey) then `--apply`, which deletes the LANDED branches; a branch
  that will not land is ABANDONED by a line in its kb/Work note (naming the branch, the word ABANDONED, the reason) and
  then `--apply --abandon <branch>`. Nothing is archived (owner 00:52: "Archiving to a bundle serves no use and wastes
  disk space").
- ⭐ **A free implementer slot is filled within the TURN it frees** — mechanically, by the rolling wave (§2,
  `templates/wf_rolling_wave.js`), whose queue is filled from `fix_clusters.py` — never at the next convenient moment. ⓜ The fix lane ran at **under 15 % utilization** of the ≤3 cap for six days
  (~2.7 mechanisms/day delivered against ~24/day of capacity) purely because slots sat empty behind landings and
  behind the evidence lane. Keep a standing queue of apply-ready contracts so the dispatch is one turn.
- A worktree-isolated agent cannot run git against the shared checkout (the harness refuses `-C`, `cd`, EnterWorktree):
  `enqueue.py land` works in the branch's own worktree, and the orchestrator removes dead worktrees itself with
  `python scripts/prune_worktrees.py --apply` (never a hand `git branch -D`: the survey's classification is the only
  thing that tells a landed branch from unlanded work, O10).
- ⛔ **NOTHING REACHES `main` BUT A TESTED TREE, AND THE SERVER ENFORCES IT.** `main` carries a REQUIRED status check —
  `ci-gate`, the terminal job of the workflow — enforced for administrators (every push here is made with the owner's
  credentials, so a rule that spares administrators spares everyone; owner decision 2026-09-06, question 23). Through the
  merge queue, `ci-gate` runs on the merge group, and a red bounces only the pull request that caused it; through
  push-main.sh, on `ci/<short-sha>` before main fast-forwards. A red is a BLOCKING finding attributed by job / step /
  failing test, fixed on its branch. **The local gate runs on ONE Windows host plus WSL; CI's runners are the other
  hosts — a test that depends on host ACL or file-lock semantics is not proven until CI is green.** ⓜ main was red for
  29 h across 16 consecutive completed runs (2026-09-05/06) while every landing in that window reported a green
  Windows-only gate and nobody looked (`kb/Work/PB796`).
- ⚠ **A docs-only landing still runs — and still has to be green.** `paths-ignore` is GONE from the workflow (it
  made the workflow decline to START, which a required per-commit check reads as "blocked forever"); the `changes`
  job now decides whether the MATRIX runs from the same path list, so a landing touching only `DEVLOG/`,
  `docs/**`, `kb/**`, `PROMPT.md`, `CLAUDE.md` or `README.md` finishes in seconds. "No run for my sha" is no longer
  an answer — it is a failure.
- Verdict batches are RE-APPLIED on the merged tree (`record_verdicts.py`), never merged as inventory JSON hunks.
- ⛔ A checkpoint file never enters a landing: every landing `git add` excludes it —
  `git add -A -- . ":!.claude/settings.local.json"` then `git reset -q -- STATUS.md`; ⛔ **not** `":!STATUS.md"`
  inside the pathspec, because STATUS.md is gitignored and `git add` EXITS 1 AND STAGES NOTHING when a pathspec
  item matches only an ignored path (measured 2026-09-06, git 2.55.0) — the form written here until now skipped
  the whole checkpoint for anyone who did not read its exit code. A lander that rebases onto a main that
  already carries a stray `STATUS.md` removes it (`git rm --cached STATUS.md`) in its commit. (2026-09-02: lander 3's
  checkpoint reached main inside a landing, and the next lander's rebase then had to choose between two agents'
  checkpoints for one path.)
- ⛔ **`git stash` IS FORBIDDEN IN THIS REPO — WIP GOES INTO A COMMIT.** The stash stack lives in the COMMON git
  directory (`git rev-parse --git-common-dir` → `E:\COBOL\.git`), so it is SHARED by every linked worktree:
  `git stash list` run from an isolated implementer worktree shows the other agents' entries and `stash pop` takes
  `stash@{0}` whoever pushed it. PB713's implementer ran `… && git stash pop -q` after a `git stash push` that had
  created nothing (its file was untracked), so the pop consumed the REGISTRAR's stash; it was restored with
  `git stash store`, and DEVLOG Entry 155 records the same shape ending worse ("Never `git stash` while background
  agents are writing files … Lost all 7 completed agents' work"). Commit WIP on your own worktree branch — that is
  what the checkpoint protocol is for — and to compare against a clean tree add a detached worktree
  (`git worktree add --detach <path> <sha>`), never stash/build/probe/pop.
- The GPL GnuCOBOL corpus (`tests/external/gnucobol`) is git-ignored and **per worktree**. The gate (`scripts/run_gate_legs.py`) fetches it
  when absent (train 23), so a fresh implementer or lander worktree measures the population from its first gate;
  `ExternalCorpusPopulationDriftTests` is RED BY DESIGN without it (`kb/Work/PB209`). ⛔ **The two population reds are NOT
  "the known worktree shape" any more** (`kb/Work/PB897`): the fetch was broken on every host whose PATH `tar` is GNU tar —
  it deleted the corpus and then failed to extract — so those reds were permanent and everyone was told to ignore them.
  The fetch now stages and swaps, tries each `tar` it can find, and prints one `FETCH FAILED: <cause>` line; a failed fetch
  makes the gate RED with `EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED` on the verdict line. **Without that line
  the population reds are a real red** — attribute them, never wave them through.
- Read-only fleets probe a **pinned worktree with its own built compiler** (`git worktree add --detach <path> <sha>`,
  build there once) so no landing swaps a binary under them.
- One comprehensive battery per landing batch, run by the orchestrator when no fleet is live — ⭐ **in a WORKTREE cut
  at the batch head**, never on main. Its phase 2 (`guard-fast`) REBUILDS, so a battery on main freezes the lander
  for ⓜ ~45 min ≈ 1.5 landings ≈ 7 clusters; run it in a detached worktree at the batch's head commit and that cost
  is zero. The cadence does not change — ⓜ a bisect over N landings costs ⌈log₂N⌉ battery runs, and neither of the
  last two non-green batteries needed one (both reds were attributed by inspection).
- **The owner's ledger artifact is refreshed after EVERY landing that moves GAP / DOCUMENTED-NON-SUPPORT / the
  actionable count, and at every battery close** (owner reminder 2026-09-02): `python scripts/spec/gen_ledger.py
  --out <html>` then an `Artifact` publish to the existing URL (recorded in the orchestrator's memory
  `conformance-ledger-artifact`). Every number is computed by the generator, never typed, and nothing on the page is
  hand-written (the in-flight narrative was removed, kb/Work PB2912): its top block is WHAT REMAINS to v1.0, and its
  trend file gains a point whenever GAP or the register's defect series moves. **Do it in the same turn as the landing report, once per landed train, not once a day** (owner
  2026-10-04: trains 1011 and 1012 had landed with no refresh). The generator back-fills one trend point per
  inventory-moving commit since the series' last point (`missed_points`), so a missed refresh costs nothing but
  a stale page. The first publish after an unseen live version is refused with that version's source; publish the
  same file again unchanged and it goes through. **After every publish run `python scripts/orchestrator/ledger_state.py
  mark-published`**; the orchestrator's supervisor prints `LEDGER PUBLISH OWED` after any unit while the page's stamp is
  newer than the marked one, because a headless unit has no Artifact tool and only renders `{COORD}\ledger.html` (owner
  2026-10-04: "Publish ledger each time"; DESIGN-orchestrator-loop.md section 14).
- `python scripts/semgrep/verify.py` has a recorded red baseline (`kb/Work/PB175`): a landing must not INCREASE it.
