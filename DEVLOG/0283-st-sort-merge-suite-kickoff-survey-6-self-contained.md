## Entry 283 — ST (sort/merge) suite kickoff: survey + 6 self-contained baselines (NIST 270→276)

Opened the ST suite (40 programs). Full survey (`scripts/run-suite.sh ST`): 17 compile+run CLEAN, 12 with
FAIL*, 1 COMPILE_FAIL (ST139A), 8 NO_OUTPUT, 2 runtime timeout, plus ST301M (flagging — asserts
"NON-CONFORMING STANDARD" diagnostics, no CCVS report, excluded like IF401M). Of the 17 CLEAN, four are
**vacuous** (000 OF 000 despite having 11–41 PERFORM PASS/FAIL cases — ST109A/112M/122A/133A) and one writes
binary into its report (ST146A); those are NOT baselined.

Baselined the **6 verified self-contained CLEAN tests** — each passes 0 FAIL* from a *clean* output dir, so
they carry no producer/consumer chain dependency: **ST104A** (1/1), **ST108A** (9/9, 8-key procedural sort),
**ST118A** (9/9), **ST119A** (27/27), **ST125A** (1/1), **ST127A** (27/27). Guard ALL GREEN: 1000 unit /
347 integration / **276 NIST** (… + 40 IX + **6 ST**), 0 regressions.

Survey leads for the rest (each its own investigation):
- **ST139A COMPILE_FAIL — FIXED (leniency L5, CBL3617/3618).** MERGE `SEQUENCE alphabet-name` with the
  COLLATING keyword omitted; `sortCollatingPhrase` required `COLLATING SEQUENCE`. The ISO format shows
  `[ COLLATING SEQUENCE … ]` in a code block (which doesn't preserve required-keyword underlining, so the
  requirement is ambiguous) — treated as a CCVS leniency: grammar relaxed to `COLLATING? SEQUENCE IS? …`,
  dialect-gated via `DialectStrictnessChecks.CheckCollatingNoiseWord` (strict modes error CBL3617; Default/
  --nist accept, warn CBL3618). ST139A COMPILE_FAIL → compiles (now 7 FAIL* — still needs the custom-alphabet
  MERGE to honor the program alphabet + other merge features). Grammar change shared by SORT/MERGE; full guard
  ALL GREEN, 0 regressions.
- **Mixed ASC/DESC procedural SORT returns COMPUTED=0** (ST101A; likely ST131A) — a SORT with mixed
  ascending/descending keys and a multi-paragraph OUTPUT PROCEDURE (`OUTP1 THRU OUTP3` spanning embedded CCVS
  boilerplate) returns zero-key records. SortRuntime *does* honor descending keys (OrderByDescending), so the
  fault is in the RELEASE/RETURN or the OUTPUT-PROCEDURE-range dispatch for this structure, not the comparator.
- **Consumer-on-missing-file hangs** — ST105A/ST117A (and likely other XXXXD00x consumers) hang at runtime
  when their producer file is absent (observed while testing standalone). A keyed/sequential read of a missing
  optional/non-optional file should give status 35 / AT END, not loop. (Harmless in the guard once ordered
  after producers, but a robustness bug.)
- **Binary report output** — ST111A/124A/126A/134A/135A emit NUL bytes into the CCVS report (COMP/binary
  record fields being displayed unconverted?).
- **MERGE "EOF NOT FOUND"** — ST147A (and the merge family ST140A/144A) fail EOF / numeric-sequence checks.
- **NO_OUTPUT** (ST102A/110A/116A/120A/123A/113M) and **timeout** (ST115A/132A) — separate runtime defects.
- Chain-dependent CLEAN consumers (ST105A/106A/107A/114M/117A/121A) can baseline once ordered after their
  TF001/TF002 producers; deferred until the producers' own tests are addressed.

