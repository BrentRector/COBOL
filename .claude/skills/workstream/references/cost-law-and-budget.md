# Workstream cost law, turn caps, grouping and the concurrency budget (SKILL.md §2, full text apart from the rolling wave and command chaining, which stay in SKILL.md)

> ⛔ **Owner standing instruction (2026-09-02).** Twenty-eight concurrent Opus agents burned ~20% of a session window
> in eleven minutes; ~fifty exhausted a window in ~2.5 h; two cutoffs in one day (the reset hour is NOT fixed — read it
> off the 429). A RESUMED long transcript re-reads its whole context on every turn, so resuming eight 300-turn
> implementers was the most expensive thing run all day, and un-checkpointed refuters lost every rule they had decided.
> "Run all this work so that we don't repeat effort when hitting a limit and resuming."

## 2. The cost law, the turn caps, and the concurrency budget

⛔ **The reason is measured, not anecdotal.** An agent's token cost is QUADRATIC in its turn count:

> **`tokens ≈ 0.115·T + 0.00031·T²`**  — fitted over **n = 239 agents**, refitted independently to
> `0.1154·T + 0.000309·T²`. ⓜ **18 of those 239 ran ≥ 250 turns and burned 39 % of all 5.62 B tokens**; re-modelled
> at a 150-turn cap the same turns cost **45 % less**.
> (`docs/rearchitecture/evidence/PROCESS-REVIEW-2026-09-04.md`; the decisions on it are `kb/Work/PB468`.)

Everything below follows from that curve. A long transcript is not "a bit more expensive" — it is the single
largest line item in the burn-down.

**Hard turn caps (owner decision, 2026-09-04).**

| agent | cap | at the cap |
|---|---|---|
| any read-only agent — adjudicator, refuter, validator, probe, reviewer | **160 turns** | checkpoint, return what is decided, name what is not |
| implementer | **400 turns** (owner, 2026-09-30, R67) | checkpoint, write `STATUS.md` `NEXT`, return a report headed `SPLIT` |

⛔ **A job that will not fit is SPLIT, never extended.** A fresh agent starting from the checkpoint is on the flat
part of the curve; the killed one's continuation is on the steep part.

**⭐ Four speed levers adopted by the owner on 2026-09-13** (asked "can we fix many known defects then run a comprehensive test?"): (1) the registrar CLUSTERS leads by root cause into one note per mechanism before any dispatch; (2) an implementer's gate is ITS OWN tests + `~Drift|~EditionGate` + the Unit assembly — the corpus and NIST legs run at the LANDER over the whole train (the train IS the "fix many, test once" step; CI and one battery per train stay); (3) ONE positive golden at the introducing edition + ONE negative below it, a copy per edition only where behaviour differs; (4) the report follows `templates/implementer-report-template.md`, ≤ 60 lines. Measured basis: a fix's gate was ~10 of its ~70 minutes; the transcript, not the tests, is the cost.

**One mechanism per implementer.** ⓜ A mechanism costs ~150 turns and an implementer's fixed cost is only ~70, so
the quadratic eats the amortization immediately: the **second mechanism in one transcript costs 44.6 M, more than
a whole fresh implementer at 40.3 M**. Two only when the notes share one code site *and* one root cause — in
which case the register's own clustering rule says they were one mechanism to begin with.

**⭐ Group related fixes — owner decision 2026-09-13 ("adopt it").** The FILL UNIT is a GROUP: the harm-ranked note plus every open note that shares its named source files or its rule family (the registrar's root-cause cluster, or a grep of the notes' code sites) — three or four notes, one implementer, one branch, one gate, one report with a section per note; the mechanism rule above still governs the WORK inside the group (each note fixed at its root, checkpoint per mechanism, SPLIT at a note boundary at the turn cap). Evidence: trains 37/38 re-merged PB416/PB391 (MOVE validity), PB419/PB420/PB425 (INITIALIZE and MOVE emitters) and PB443/PB877 (the subscript resolver) pairwise, two composition defects appeared that neither implementer could see, and half the reports folded a sibling note. A group touching a shared seam runs `~CorpusRunner` + `~Nist` at the implementer's gate (the PB425 drop).
⭐ **THE GROUPS ARE COMPUTED, NOT EYEBALLED — owner 2026-09-27 ("group fixes so all fixes in one source file, or one
small set of related code, all get fixed in one pass").** `python tools/claude-skills/skills/agent-fleet/references/fix_clusters.py [--json out.json]`
resolves every open defect's named code sites to real `src/Cobol.Net.*` files, gives each note a PRIMARY file, and
clusters notes by it (cap 5, split by harm, singletons absorbed into a cluster whose file they also name), ranked by
summed harm. **Fill each slot with the top-ranked cluster of a subsystem not already in flight**, never a hand-picked
lead + keyword siblings. Measured on its first run: 411 open actionable defects → 125 clusters (42 of five notes), so
a six-slot wave carries ~25–30 defects instead of ~10. Wave 65, grouped by hand, split `ConditionRenderer.cs`'s five
defects across two implementers (A and E) and left three of `ReferenceFormatProcessor.cs`'s five behind.

⭐ **ORIENTATION IS PAID ONCE, NOT ONCE PER WAVE — owner 2026-09-27 ("over many waves, more of the same orientation
… can we reduce the repeated work?").** Every dispatch spec tells the implementer to run `python tools/claude-skills/skills/agent-fleet/references/orient.py
<the files its notes name>` BEFORE reading any source (MANDATORY-PRACTICES P6; `check_practices.py` refuses a spec
without it). It prints each file's outline with line numbers, cited clauses, covering tests, what LANDED notes learned
about the file (their code sites and mechanisms, newest first), the open notes naming it, and recent commits. It is
derived on each run and never goes stale. Measured baseline (wave 65, 7 transcripts, 1,401 turns): 63 % of tool calls
were reads/searches, 15 % of turns and 43 % of tool-result bytes came before the first edit. Estimate: ~25 turns saved
per implementer (≈ 200 → 175), about **15 % of its tokens** on the cost law, about one weekly point per six-slot wave.
Verify on wave 67 with the same measurement (turns to first edit, read calls, pre-edit bytes).

**⭐ Lander throughput — owner decision 2026-09-22 ("do all that we can").** The lander is the serial bottleneck: train
47's lander took 84 min, and its whole-Conformance leg measured 9.6 min on a quiet host, 18.5 min at battery #84 and
30.6 min in train 48, because up to seventeen implementers were running the SAME whole assembly on the same 32 cores.
(1) Every implementer gate is `build-local.ps1 -Mode implementer`, the ORDERED whole population (owner, 2026-09-28,
kb/Work PB1708: "order, don't skip"), and it holds a GATE SLOT (`scripts/gate_slot.py`, kb/Work PB1720): at most N
implementer gates build or test at once, repository-wide (N is one shared setting, `gate_slot.py set-cap`, kb/Work
PB2514), while the lander (`-Mode lander`) never takes one and never waits. During the owner's batched-gating trial
(kb/Work PB2515, until Sat 2026-10-10 10:00 PDT) the implementer gate runs leg 1 only and the lander's train gate is
the one whole-population run per train (MANDATORY-PRACTICES L12): ≈ 2–3 whole-population runs per 5-change train
against ≈ 7–8, measured per train by `scripts/orchestrator/train_measure.py`. (2) Every implementer gate runs at `-Priority BelowNormal`, which Windows passes down to the build, the test
hosts and every compiled program, so the lander's Normal-priority gate wins the cores. (3) PIPELINED
LANDING UNDER ONE LANDING LEASE (kb/Work PB2537): the next train's lander is dispatched while the previous one is still
in CI and brings its clusters in at once, then takes the landing lease (`scripts/orchestrator/landing_lease.py`), which
waits for the previous lander's push-main to release it, and only then makes its final rebase and runs its gates, so
it gates once and never loses the race to main (the R1 lander lost it three times on 2026-10-07). CI remains the proof.
(4) Trains stay at 4–6 clusters. (5) The Conformance runner's compiled-program cache (kb/Work PB985) makes a
re-gate after a test-only fix skip recompilation.

**One landing per lander transcript.** ⓜ lander-3 carried 4 landings in 794 turns for **295.9 M**; the same turns
split into four fresh ~198-turn landers model at **140 M — 53 % less**. (What a lander may batch is *clusters
inside one landing* — §3.)

**The budget: 1 lander + ≤6 implementers + one 4-wide read-only chunk (≈11 agents).** ⭐ The implementer cap was
**raised from 3 to 6 by owner decision on 2026-09-05** (`kb/Work/PB468` Q4), on the measurement that the 3-slot lane ran
at ~100 % utilization all morning while the lander sat idle half the time (≈48 min per implementer, ≈33 min per
four-cluster landing); six implementers saturate one lander with five- or six-cluster trains, which is the band above.
⛔ **Fill the six slots ONE GROUP PER SUBSYSTEM** (a group per the paragraph above; formerly one item) (the top of `work.py next` for each of binding, codegen, frontend/grammar,
runtime, io …), never six items down the rank list: twelve consecutive file-I/O clusters on 2026-09-05 forced serial
branches, a six-conflict mid-flight merge (~70 turns) and measured-overlap manifests for every train. It is an
error-containment cap, and ⓜ it is TOKENS, not slots, that the weekly limit rations — so the budget is a default,
while the caps above are what actually control spend. Workflows take `concurrency`/`refuteConcurrency` args and run
chunks with `parallel()` inside a loop, never a 16-wide `pipeline`. A fleet over ~40 agents is the signal to SPLIT
the work, not to wait. Stage the earliest-stage, largest jobs behind the near-done ones.
