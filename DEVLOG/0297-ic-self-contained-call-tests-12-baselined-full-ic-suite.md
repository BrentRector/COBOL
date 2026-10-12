## Entry 297 — IC self-contained CALL tests: +12 baselined, full IC suite mapped (NIST 305→317)

Tackled the resume-prompt's biggest-headroom suite (IC, inter-program CALL). The roadmap flagged IC
"CLEAN" as vacuous-prone — a caller whose callee is a *separately-compiled* program would, under the
guard's one-`.cob`→one-`.dll` build, either not link or silently no-op while still printing PASS. So
first I mapped all 47 IC files by whether their **first/main** program is a caller (has a CCVS report)
or a callee subprogram (`PROCEDURE DIVISION USING` at the top, no report):

- **~23 are callee-only halves** (IC102A/104A/105A/107A/109A/110A/111A/113A/115A/117M/118M/202A/204A/205A/
  206A/208A/210A/211A/212A/214A/215A/217A) — invoked by a caller, NO_OUTPUT standalone. Excluded by design.
- **~24 are standalone callers** with a CCVS report. The CCVS convention concatenates the caller **and its
  contained callee program(s) into ONE `.cob`** (e.g. IC101A.cob holds IC101A + IC102A; IC203A.cob holds
  IC203A/204A/205A/206A) — so the single-dll build *does* contain the callee and the CALL resolves at
  runtime. (A separately-named `IC102A.cob` etc. also exists — that's the callee-only half above.)

**Baselined 12** self-contained callers (joining the 4 already in the guard → 16): IC101A, IC103A, IC108A,
IC112A, IC201A, IC209A, IC213A, IC216A, IC222A, IC223A, IC226A, IC237A. Each verified **non-vacuous**, not
just 0 FAIL\*:

- **The CALL transfer is provably real.** IC101A's callee IC102A does `ADD 1 TO DN2 (state); MOVE DN2 TO
  DN1 (a LINKAGE param BY REFERENCE)`. The caller checks DN1=1, then re-zeroes it and checks DN1=2, then
  DN1=3 across three successive calls — which passes ONLY if (a) CALL USING transfers BY REFERENCE so the
  caller sees the callee's write, AND (b) the callee retains state between calls (ISO §14.9.10 — a CALLed
  program is not re-initialized per call). A no-op CALL fails test 2. 5/5.
- **CALL by identifier** (IC201A/223A): `ID1 PIC X(6) VALUE "IC202A"` + `CALL ID1 USING …` resolves the
  program name from a runtime data item, callee transforms 4 params. 11/11.
- **ON OVERFLOW / ON EXCEPTION discrimination** (IC222A): CALL-TEST-1 calls an *available* program (overflow
  must NOT fire) while CALL-TEST-2 calls a *non-existent* one (overflow MUST fire) — both pass, so our CALL
  genuinely detects missing programs rather than uniformly no-opping. 16/16.

All 12 deterministic across two clean runs, 0 FAIL\*, EXECUTED>0. Guard ALL GREEN: 1000 unit / 347
integration / **317 NIST**, 0 regressions.

**Two real bugs found (deferred, task tracked):** IC106A (two tables with INDEXED BY + an INDEX data item
across CALL) and IC207A (variable-length OCCURS DEPENDING table + condition-names in a LINKAGE param) both
**segfault** (rc=139) inside `IC*.Dispatch` with pointer-based `CobolDataPointer[]` args — a shared root
cause: CALL USING an index/table parameter addresses out of bounds. **Remaining IC** after this: IC106A/
IC207A (the index/table-param crash), IC114A (file-I/O chain consumer), IC227A/IC235A (EXTERNAL clause),
IC233A/234A (`USE GLOBAL AFTER ERROR PROCEDURE` declaratives) — all genuine Phase-4 work; IC116M/IC401M
are flagging modules (no CCVS report, excluded).

