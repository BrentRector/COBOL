# Workstream landing order and mechanics (SKILL.md §3, full text)

## 3. Landing order and mechanics

- **Land finished work first**; read-only fleets are staged behind landings. **One lander on main at a time** (DEVLOG
  numbering and fast-forward ordering); **one LANDING per lander transcript** (§2) — split the queue into fresh agents.
- ⭐ **FIVE CLUSTERS PER LANDING (target 5; 4–6 is the band).** A landing is ~90 % fixed cost — bring the work in,
  build, gate, DEVLOG, commit, push — so ⓜ **10.4 M per cluster at k = 1 against 5.1 M at k = 5**, and 4.0 minutes
  of lander per cluster against 9.8. The corpus proves it directly: the golden lander landed 151 rows for 52.9 M =
  0.35 M/row; the PB383 lander landed 2 rows for 7.2 M = 3.6 M/row — **10×**. Mechanics: bring in each implementer's
  diff in turn, **one build**, the **WHOLE population of Conformance, Unit and Characterization in one leg** (`build-local.ps1 -Mode lander`; a union of the implementers' filter terms was never a landing gate — CI's `rest` shard is exactly the tests no term names, and it was red on the first push of trains 39, 40 and 41 in one day), **one commit per cluster inside the landing** so
  a red is attributed per cluster by re-running only its failing cases on each cluster alone, then fixed in the train
  or ejected to a finisher, an interaction bisected (MANDATORY-PRACTICES L12, kb/Work PB2515), one DEVLOG entry naming
  every cluster, one push, and the train recorded with `scripts/orchestrator/train_measure.py`. Past six clusters the token curve is
  flat and the gate-attribution risk is not. Template: `templates/lander-train-brief.md`.
- ⛔ **Never spend a lander on a 1–2 cluster landing** unless nothing else is ready — that is the k = 1 corner of the
  table above, at twice the cost per cluster. If only one cluster is finished, hold it and dispatch the lander when
  the train is full; a blocking fix is the exception and is landed alone on purpose, said so in the report.
- ⭐ **A free implementer slot is filled within the TURN it frees** — mechanically, by the rolling wave (§2,
  `templates/wf_rolling_wave.js`), whose queue is filled from `fix_clusters.py` — never at the next convenient moment. ⓜ The fix lane ran at **under 15 % utilization** of the ≤3 cap for six days
  (~2.7 mechanisms/day delivered against ~24/day of capacity) purely because slots sat empty behind landings and
  behind the evidence lane. Keep a standing queue of apply-ready contracts so the dispatch is one turn.
- A worktree-isolated agent cannot run git against the shared checkout (the harness refuses `-C`, `cd`, EnterWorktree):
  landers gate in THEIR worktree, then `git fetch origin && git rebase origin/main` and land with
  **`bash scripts/push-main.sh`**; the orchestrator runs `git merge --ff-only origin/main` locally and removes dead
  worktrees itself (`git worktree remove --force`, `git branch -D`).
- ⛔ **THE ONLY WAY A COMMIT REACHES `main` IS `bash scripts/push-main.sh`, AND THE SERVER ENFORCES IT.** `main`
  carries a REQUIRED status check — `ci-gate`, the terminal job of the workflow — with `enforce_admins: true`, so a
  bare `git push origin HEAD:main` of an unverified commit is REFUSED. Admin exemption was never an option: every
  push here is made with the owner's credentials, so a rule that spares administrators spares everyone, and the
  orchestrator's own pushes obey this one (owner decision 2026-09-06, question 23). The script pushes HEAD to
  `ci/<short-sha>`, watches the run for that exact sha, and only then fast-forwards main and deletes the branch; on
  a red it prints the failing jobs and exits non-zero with main untouched.
  ⭐ It is IDEMPOTENT — a full-matrix run is ~25–30 min, longer than a command timeout, so a cut-off caller simply
  re-runs it (or runs it with `run_in_background`); a sha that already carries a green `ci-gate` skips straight to
  the main push. A red is still a BLOCKING finding, attributed by job / step / failing test in the report, and the
  orchestrator lands that fix alone before the next train. **The local battery runs on ONE host; the CI's Linux job
  is the only Linux gate — a test that depends on host ACL or file-lock semantics is not proven until CI is green.**
  ⓜ main was red for 29 h across 16 consecutive completed runs (2026-09-05/06) while every landing in that window
  — trains 10 through 20 — reported a green Windows-only gate and nobody looked (`kb/Work/PB796`).
- ⚠ **A docs-only landing still runs — and still has to be green.** `paths-ignore` is GONE from the workflow (it
  made the workflow decline to START, which a required per-commit check reads as "blocked forever"); the `changes`
  job now decides whether the MATRIX runs from the same path list, so a landing touching only `DEVLOG.md`,
  `docs/**`, `kb/**`, `PROMPT.md`, `CLAUDE.md` or `README.md` finishes in seconds. "No run for my sha" is no longer
  an answer — it is a failure, and `push-main.sh` treats it as one.
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
  --out <html> --in-flight <md>` then an `Artifact` publish to the existing URL (recorded in the orchestrator's memory
  `conformance-ledger-artifact`). Numbers are computed by the generator, never typed; only the in-flight narrative is
  hand-written. **Do it in the same turn as the landing report, once per landed train, not once a day** (owner
  2026-10-04: trains 1011 and 1012 had landed with no refresh). The generator back-fills one trend point per
  inventory-moving commit since the series' last point (`missed_points`), so a missed refresh costs nothing but
  a stale page. The first publish after an unseen live version is refused with that version's source; publish the
  same file again unchanged and it goes through. **After every publish run `python scripts/orchestrator/ledger_state.py
  mark-published`**; the orchestrator's supervisor prints `LEDGER PUBLISH OWED` after any unit while the page's stamp is
  newer than the marked one, because a headless unit has no Artifact tool and only renders `{COORD}\ledger.html` (owner
  2026-10-04: "Publish ledger each time"; DESIGN-orchestrator-loop.md section 14).
- `python scripts/semgrep/verify.py` has a recorded red baseline (`kb/Work/PB175`): a landing must not INCREASE it.
