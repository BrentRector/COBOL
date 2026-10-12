## Entry 1914 — 2026-10-07 05:05 PDT — The census findings are register notes: 103 Delete, Unify and Move/rename items filed by a generator (PB2119's program; wave 1030, a Sonnet clerk in two minutes)

**What landed.** `scripts/arch/file_census_notes.py <findings.json> --ids <ranges>` turns the R0 census's findings into
`kb/Work` notes and is idempotent (a finding whose `R0-` id already appears in a note's `census_ids:` line is skipped: a
second run filed 0 and skipped 115; `--self-test` proves it on a two-finding fixture). Run over
`docs/rearchitecture/evidence/arch-census/a02b165dae86b674e16399468144e1386171236b.findings.json` it filed 103 notes,
PB2155–PB2257, all `kind: analysis`, `cluster: ["PB2119"]`, so `plan_wave.py --cluster PB2119` can plan them: 9 **Unify**
(one per clone family, each marked NEEDS-OPUS because the census's own target says "triage semantically"), 75 **Delete**
(9 dead-artifact notes grouped by class and directory, 65 unreferenced-member notes one per source file, 1 unreferenced
type) and 19 **Move/rename** (one per folder-namespace disagreement). Each note carries the finding's sites verbatim, how
it was measured (a pointer at the census record, never a restatement), the wave's contract (every caller in the same
change, the drift test travels, the oracle proves neutrality, a behavior change is a defect, never a deletion) and the
model that may take it. Not filed, by design: the 52 god-class findings (their targets come from R1, PB2118) and the 67
test-only findings (a judgment — delete, move to tests, or a pinned contract — listed as NEEDS-OPUS in the report
`E:\COBOL-coord\scratch\reports\w1030-PB2119-filing-report.md`). The findings file carries cp1252 mojibake in its
text (`Â§`); the generator repairs it. Unused ids PB2258–PB2274. The clerk (Sonnet, `cobol-clerk`, 18 tool calls,
79,685 tokens) was dispatched by the attended session from a brief file, not the orchestrator loop, which plans
implementers and landers only.

**Why a generator.** 115 hand-written notes would have cost the clerk its turn cap and the next census run the same
cost again; one script over the data is the shape that makes the next case automatic (CLAUDE.md rule 5), and its
idempotency key is the census's own finding id.

