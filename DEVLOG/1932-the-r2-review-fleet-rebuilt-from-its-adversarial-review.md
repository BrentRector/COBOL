## Entry 1932 — 2026-10-07 23:06 PDT — The R2 review fleet rebuilt from its adversarial review (PB2558–PB2561): computed subsystems, shards, inputs once, per-decision checkpoints, three-lens skeptics, scripted filing

The owner held R2's first batch at 20:40 PDT for a Mythos adversarial review of its brief
(`E:\COBOL-coord\scratch\reports\w1034-R2-brief-adversarial-review.md`: seven blocking, seven non-blocking). The
batch that had started at 20:45 with eight Opus agents was discarded: its reviewers wrote their files only at the end.
This entry is the rebuild, as tracked tooling with self-tests, so the next batch is a command.

- **B7.** The census and member index of the pin 8be230068 are recorded
  (`docs/rearchitecture/evidence/arch-census/8be23006851a…{json,findings.json,members.json.gz}`): 1,447 authored types,
  53 god-class candidates, 9 clone families, 370 unreferenced and 116 test-only members, 222 findings.
- **B1, B2 (PB2558).** `scripts/arch/r2_subsystems.py` is the subsystem table — 17 subsystems (the runtime's
  collation, Unicode and globalization is the seventeenth; the source generator joins the bound tree) — with a
  partition check (1,503 files at the pin, 0 holes, 0 overlaps), balanced shards of at most 6,000 physical lines (75),
  and the design block rendered from it. `ArchReviewFleetDriftTests` holds the partition and the block on the
  committed tree.
- **B4, N2, N3 (PB2559).** `scripts/arch/r2_inputs.py` runs semgrep, the analyzers at `latest-all`, the drift-rule
  query and the open-notes scan once per pin and writes one input per shard; duplication is one whole-codebase clone
  pass; performance needs a measurement and modern C# an analyzer rule id, else a lead.
- **B3, B5, B6 (PB2560).** The workflow moved into `.claude/skills/workstream/templates/wf_r2_review.js`
  (`check_practices.py` requires its parts; `scripts/arch/test_wf_r2_review.mjs` dry-runs it): a JSON line per
  decision, a finisher for the unread remainder, an examiner for every null, three lens skeptics per chunk of four
  findings. `scripts/arch/r2_collect.py` decides from disk; `file_census_notes.py --r2` files upheld findings as
  PB1754 notes with ids from `alloc.py`.
- **N1, N4–N7 (PB2561).** The `cobol-reviewer` role (Opus, 120 turns, read-only hook); criteria in one place; the
  authorization quoted correctly; one cluster name, PB1754; the watchdog and the budget read in "Running a batch".
- Found on the way and fixed here: the fleet-tooling check (`check_practices.py`) reads every script for submodule
  paths, so a self-test fixture naming a planted submodule file turned it RED; the fixture now builds the path.
- Also found and fixed: the analyzer step attached to the fleet gates' shared MSBuild nodes and compiler server and sat an hour with the machine 40 % idle; isolated, csc on `Cobol.Net.Compiler` at `latest-all` ran past 2,000 CPU-seconds and 28 GB while the leaf projects took under a minute. `r2_inputs.py` now measures one project at a time (cached), and a shard's input waits only for its own projects (`--projects`); batch 1's inputs exist, the compiler's and the tests' are pending.

- Also fixed: `scripts/prepend-devlog.py` anchored on CRLF + `## Entry`, so an LF-preceded newest heading
  put this entry below its predecessor; it anchors on the LF now.

DESIGN-architecture-review §3 R2 describes the fleet as built. Batch 1 (the small shards: runtime values, editions
and diagnostics, the CLI, and the whole-codebase clone pass) is ready to launch.

