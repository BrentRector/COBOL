## PB3175 — the conformance suite's harness (slice S1b), branch review round 1: an `--editions` record keeps the other editions, and the WiseOwl adapter reads a diagnostic of any code

kb/Work PB3175, design `docs/rearchitecture/DESIGN-conformance-suite.md` §5 and §13 S1b. Entry 2143 landed S1b as
its implementer left it (through PB3176's branch, which merged that head); this landing carries the four findings of
S1b's branch review, round 1, which only this branch held.

**The findings (all fixed).** (R1-1) `run.py --record-baseline --editions 2014` rewrote the whole baseline with that
one edition, erasing every other edition's recorded verdicts: an `--editions` run now replaces only the record of
each edition it ran (each edition's record is a whole run of its suite), with a self-test arm that was red on the old
code. (R1-2) The WiseOwl adapter's diagnostic pattern required `COBOLNET<n>`, so a parse error (`COBOL0001`) or a
COPY error (`CBL<n>`) at a flagged test's marked line read as "no diagnostic reported": the pattern now takes any
code at its position, and the new `ConformanceSuiteAdapterDriftTests` renders every code of both registries through
the compiler's own formatters (`Diagnostic.ToString`, `EditionContext.Error`/`Warning`) and requires the pattern to
read each one's file and line (51 misses on the old pattern). Because PB3176's in-process runner reads the adapter's
pattern (`ConformanceSuite.Diagnostic`) instead of restating it, both runners now read the wider pattern. Sibling:
`gnucobol_differential.py`'s `ourCodes` dropped the CBL family, now included. (R1-3) The self-test ran its PASS arms
under a 3 s hang guard, a wall-clock limit on a stub process: the two hang arms now run as a suite of their own under
the short guard (NO VERDICT however loaded the host), every other arm under the default guard, and the self-test
takes 7 s instead of 29. (R1-4) `kb/Work/PB782` records that its target (3), the exit-code mapping, landed with S1b
(re-measured: COBOLNET0900 and COBOLNET1639 exit 65) and stays open for the end-to-end CLI test.

**The landing's conflict (finisher).** The first `enqueue.py land` (2026-10-17 01:15 PDT) stopped on a conflict in
`conformance-suite/harness/generate.py`: S1b's implementer had built on PB3174's implementer head, and both S1b's
generator changes and PB3174's review round 1 (`Reader._library_text`, the reworked self-test arms) rewrote the same
regions. PB3176's branch resolved that conflict and main took it with PR 177, so the merge of main now takes
`generate.py` cleanly. Two files conflicted instead: `conformance-suite/adapters/wiseowl.json`, where main added the
locale pin (`environment`, PB3176) beside the old `COBOLNET`-only pattern, resolved to the wide pattern AND the pin,
with both sides' `$comment` lines; and the pending entry for S1b, which main had already stamped as entry 2143: it
keeps that number, and this entry carries what 2143 lacks.

**Tests.** `run.py --self-test` 59 arms, `generate.py --self-test`, `ConformanceSuiteAdapterDriftTests`,
`ConformanceSuiteRunnerDriftTests` and `ConformanceSuiteTests` (the in-process runner on the merged adapter).

No inventory row: test infrastructure (`closes_rows: []`, PB3175's reason filed).
