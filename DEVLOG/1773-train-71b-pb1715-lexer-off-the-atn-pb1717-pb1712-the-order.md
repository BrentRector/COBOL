## Entry 1773 — 2026-09-29 00:11 PDT — Train 71b: PB1715 (lexer off the ATN), PB1717+PB1712 (the order plan); PB1719 dropped on a Linux CI red

Train 71b set out to carry the rest of wave 71 of the PB1708 "order, don't skip" pivot:
- group A (M6);
- group F (M12), whose branch carried its same-file predecessor, group C (M11), by fast-forward.

C and F were split into separate commits so that a bisect lands on one mechanism. All three cut from `bf2e5b43f`.
Train 71 had landed on top (`b6c8a25e6`), so the patches met its M14 and M2 edits in the same design sections.
**F was DROPPED on a Linux-only CI red**, and A and C landed.

**PB1715 (M6).** Six left-edge semantic predicates in the lexer's DEFAULT mode stopped ANTLR from caching two things:
that mode's start state, and every DFA edge whose target went through a predicate. The six were the four
FN_SIGNED_* twins, DEFINED and FNARG_SEPARATOR. As a result, every token of every compile re-ran ATN simulation
and re-added its DFA state under `lock(dfa.states)`, and parallel compiles serialized on that lock.

Each match is now unconditional, and an ACTION decides it:
- `OnSignedLiteral` keeps the signed literal where §8.3.3.3.2 2) and §8.7.1 make the sign part of it. Anywhere else
  it cuts the token back to its PLUS/MINUS sign, re-seeks the input and restores the column.
- FNARG_SEPARATOR `Skip()`s outside an argument region (§8.3.5 2)), so COMMA_SEP is deleted.
- DEFINED is a virtual token that IDENTIFIER's action retypes in a primed region.

The implementer first made DEFINED a literal rule that retyped itself, and the gate went red. `CobolWordsDriftTests`
caught that the rule's vocabulary literal name would make `CobolKeywordTokens` treat DEFINED as a keyword.

The one-time differential covered all 17,055 distinct lexer inputs that the three assemblies lex, captured on the
old grammar with the compile cache off. Replayed through both lexers, 9,955,488 tokens were identical in type,
channel, offsets, line, column, text and final mode.

The new `LexerDfaCacheDriftTests` uses an input-side DFA-walk probe, because ANTLR's simulator has no virtual seam.
It is red on the old grammar and on a planted mid-rule predicate, and green in 3 s.

Measurements:
- b2: 1 thread went 110.1 s → 3.4 s; 12 threads take 2.0 s.
- CPU was 21 %, so the ≥ 70 % target was not met. Workstation GC is now the limit (PB1727).

The inventory batch retired the deleted COMMA_SEP as a witness of SR-8.3.5-1, which stays CONFORMS.

**PB1717 + PB1712 (M11).** `scripts/spec/impacted_tests.py` no longer narrows the gate, which closes PB1712. It now
only TIERS a change: its filter line is always `FullyQualifiedName~.`, and `--plan` writes the analysis.
`scripts/gate_plan.py` is new. It builds `plan.json` from `NameKey`, tiers 0a/0u/1–3, the budgets and the 15 s
collection cap.

The acceptance run corrected the design (rule 5):
- Tier 0a used to be "every case declared in a changed test file". That put 5,774 cases in 0a for 68b X, and the
  first red came after 62.8 % of the work.
- Tier 0a is now the test methods the diff ADDS. The first red now comes after 0.32 %, and b7 gives one tier-0u
  case and a 15.3 s floor.

Every caller of the old API moved in the same change, including the evidence scripts (`common.py`, `a2`, `a5`–`a8`,
`b4`). `ImpactedTestsDriftTests` (5 facts) pins both self-tests and the partition-suffix contract.

**PB1719 (M12) — DROPPED.** The first push-main of the three-cluster train (head `4fdc6897c`, CI run 36533008383) was
RED in exactly one place:
- job `Greenfield unit + characterization (Linux)`, step `Greenfield unit tests`;
- `GateLegDriftTests.Arm5_TheRootAudit_FiresOnPlantedOffenders`: expected 3, actual 0.

It is F's new witness. `GateLegAudit.RootCarrying` normalizes the root with `Path.GetFullPath`, and the planted
Windows root `E:\COBOL-wt\battery87` is a relative path on Linux. So no planted offender can match there.

The lander's Windows gate of all three clusters had been GREEN (9321 / 29755 / 35), and main was never touched. F
was dropped, not fixed by the lander. Its content, including the lander's union of `GateLegDriftTests` with
PB1718's arms 3 and 6, is on local branch `train71b-with-F-red` (commit `26fc0b85a`). The drop is recorded in the
PB1719 note, which stays open.

**The train.**

Merges:
- `DESIGN-test-build-ci.md` §3.14.9: the M11 row now carries its LANDED measurements, and train 71's M14 row keeps
  its as-landed text.
- `docs/DRIFT_RULES.md` was regenerated at each step, never hand-merged.

Gate of A+C on the reset tree, whole assemblies:
- `Passed! 9319/9319 Conformance` · `Passed! 33/33 Characterization`.
- Unit had one red: `ConflictMarkerDriftTests.TheSweepActuallyReadsTheTrackedTree`, an `UnauthorizedAccessException`
  on a tracked `.cob` minutes after `git reset --hard`. That file was readable immediately afterwards, and the whole
  Unit re-run was `Passed! 29745/29745`. The red is filed as PB1729, not waved off.
- The legacy `CobolSharp.Tests.Integration` had passed 503 of 504 (1 skipped) on the three-cluster tree.

Checks:
- The audits are clean: code and doc citations 0, evidence supersession 0, witness loss 0 unexcused (1 retired:
  COMMA_SEP).
- `drift_rules --check` passes (230), and so does `work.py check` (1637).
- semgrep passes, with only `raw-diagnostic-code-literal` 301 → 300 DOWN, as on main.
- `cite.py --check` passes for §8.3.3.3.2 2), §8.7.1 and §8.3.5 2).

Review: 0 findings. Clusters dropped: 1 (F, on the CI red above). GAP 866 → 866.

Leads filed:
- PB1727: the test hosts' GC mode.
- PB1728: PIC_STRING's seek-back leaves the column +1. It predates M6: the differential was identical in column.
- PB1729: the transient ConflictMarker red.
- PB1721 (M13) gained the C and F handoffs.

