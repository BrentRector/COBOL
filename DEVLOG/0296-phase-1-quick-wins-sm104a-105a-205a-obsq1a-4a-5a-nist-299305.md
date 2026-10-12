## Entry 296 — Phase 1 quick wins: SM104A/105A/205A + OBSQ1A/4A/5A (NIST 299→305)

Worked the resume-prompt's "Phase 1 — quick baseline wins" list, and immediately hit the trap the
roadmap warned about: **`run-suite.sh` reports a chain consumer as CLEAN by reading a STALE shared
file** (it only `rm`s the per-test print file, never the `TF###` data files, so a consumer left over
from a prior guard/run-suite invocation reads whatever happens to be on disk). Every "CLEAN" candidate
therefore had to be re-verified from a genuinely clean state with its producer ordered ahead — never
trusting the survey's verdict.

- **SM105A / SM205A — self-contained, baselined.** Both are SORT-description COPY tests (COPY SORT DESCR /
  COPY SD REPLACING) that create their own scratch file via `SORT … GIVING` (SORTOUT assigned `XXXXX001`)
  and read it back. 9/9 executed, 0 FAIL\*, deterministic across two clean runs. Added at the end of the
  SM block.

- **SM104A — chain consumer of SM103A, baselined (NOT a compiler bug).** Initially looked like a real
  `DECIMAL-POINT IS COMMA` failure (1 FAIL\* on COPY-TEST-3 in isolation; transiently 5 FAIL\* when my
  first compile raced the background guard's rebuild). Root cause: `SM103A.cob` is a **two-program file** —
  the SM103A *producer* (writes `TF001` via `XXXXP001`) followed by the SM104A *consumer* (reads it via
  `XXXXD001`). SM103A's BUILD section moves `S-N-1` (PIC 9(8)V99 VALUE 12345678,91, i.e. 12345678.91 under
  the copied `DECIMAL-POINT IS COMMA` from K3SNA) through `S-N-2` (PIC ZZ.ZZZ.ZZZ,99) → "12.345.678,91"
  and writes it as record 1's TST-FLD-2, with RCD-1..7 (97532, 23479, …) as TST-FLD-1. With SM103A run
  first, SM104A is CLEAN 7/7 — so our compiler's `DECIMAL-POINT IS COMMA` numeric editing round-trips
  correctly through a file. The whole "bug" was producer ordering. (My first wrong guess used SM101A as
  the producer — but SM101A→SM102A is a *different* chain whose record is `COPY K101A` = TST-FLD-1 only.)
  Placed SM104A immediately after SM103A in the guard.

- **OBSQ1A / OBSQ4A / OBSQ5A — obsolete-sequential, baselined; OBSQ3A is a producer.** OBSQ1A (standalone)
  tests the obsolete `VALUE OF`/`LABEL RECORD` FD clauses, 6/6. OBSQ3A/4A/5A test the obsolete
  `MULTIPLE FILE CONTAINS` I-O-CONTROL clause (tape-volume positioning). OBSQ3A creates the multi-file
  "tapes" and executes 0 tests → it's a pure **producer/builder** (class-2, runs ahead, not baselined, like
  the 10 ST producers). OBSQ4A (4/4) and OBSQ5A (5/5) read those tapes back and verify 750 records per
  logical file — genuinely non-vacuous round-trips, not blank-vs-blank. `MULTIPLE FILE`/`VALUE OF` are
  documentation-only clauses with no observable I/O effect on modern storage, so accepting-and-ignoring
  them while the data round-trips is spec-conformant, not vacuous. Ordered OBSQ3A→OBSQ4A→OBSQ5A (plus
  OBSQ1A) as a self-contained block at the end of NIST_TESTS.

Guard ALL GREEN: 1000 unit / 347 integration / **305 NIST** (was 299; +SM104A/105A/205A +OBSQ1A/4A/5A,
+OBSQ3A producer). 0 regressions. **Process note worth keeping:** for any chain consumer, verify from a
clean output dir with the producer ordered ahead — `run-suite.sh`'s CLEAN is unreliable because it never
cleans `TF###`. SM104A and OBSQ both confirm the IX/ST-era lesson: a "CLEAN consumer" is almost always a
producer-ordering question, not a compiler defect.

