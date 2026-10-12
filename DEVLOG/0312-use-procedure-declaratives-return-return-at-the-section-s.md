## Entry 312 — USE-procedure declaratives-return: return at the section's exit, not its termination tail → SQ212A baselined (NIST 349→350)

Entry 311's boundary fix left SQ212A running clean but re-printing the CCVS "END OF TEST" footer **14×** (once
per status-44 event). Root cause — the `USE AFTER STANDARD EXCEPTION PROCEDURE ON SQ-VS7` declarative's
return point.

**The dispatch model.** A USE procedure is lowered (`FileIoLowerer.EmitPerformDeclarativeSection`) as
`IrPerformThru(firstPara … lastPara)` over the declarative section, executed by the return-address
`Dispatch(startPc, exitPc)` engine, which returns only when the **exitPc** paragraph completes by falling
through to `exitPc+1`. It set `exitPc` = the section's **physical last paragraph**. CCVS declaratives for the
I/O suites place a TERMINATION TAIL — `EXIT-PARA. EXIT.` → `CLOSE-FILES1.` (`PERFORM END-ROUTINE1 …` = the
footer, then `CLOSE PRINT-FILE`) → `TERMINATE1-CCVS.` (`STOP RUN`) — at the end of the *same* section. So the
handler's normal-return `GO TO EXIT-PARA. EXIT.` fell THROUGH into `CLOSE-FILES1` and re-printed the footer; the
trailing `STOP RUN`, executing inside a *nested* Dispatch, returned rather than halting the run unit, so the
program continued and repeated this on every exception. (RL119A has the same skeleton but is unaffected because
its handler `GO TO`s the *abort* path `D-CLOSE-FILES`, which fires once and terminates — a different exit than
SQ212A's return path.)

**The fix is to end the USE procedure's PERFORM THRU at the declarative's designated exit, not the section's
physical last paragraph — but identifying that exit took two wrong heuristics first (transparency):**
- **(1) "last bare-EXIT paragraph"** — broke **17 SQ tests**. SQ133A returns via `GO TO END-DECLS` where
  `END-DECLS.` is an **empty** last paragraph; ignoring empties mis-picked an earlier `DECL-BAIL-EX` (a nested
  `PERFORM … THRU` target) as the exit → the section ran into the main program → 109 k-line runaway report.
- **(2) "last *trivial* paragraph" (empty OR bare-EXIT)** — fixed the SQ133A family but still broke SQ141A/225A:
  SQ141A's `END-DECLS.` (its real exit, the last paragraph) is **non-trivial** (`MOVE ZERO TO DECL-EXEC-SW`), so
  "last trivial" again mis-picked an earlier EXIT.
- **(3) correct — termination-tail detection.** The exit is the section's last paragraph by default; it is the
  last trivial exit-point paragraph **only when a terminating paragraph (STOP RUN / EXIT PROGRAM / GOBACK)
  FOLLOWS it** in the section (the tail the handler must not fall through into). This precisely isolates SQ212A
  (`STOP RUN` in `TERMINATE1-CCVS` after `EXIT-PARA`) from SQ133A/SQ141A (no trailing terminator → unchanged,
  the last paragraph IS the handler's exit). A new `Binder.ScanDeclarativeControlPoints` pre-pass populates
  `LoweringContext.ExitPointParagraphs` (no statements, or solely `BoundExitStatement`) and
  `TerminatingParagraphs` (`BoundStop`/`BoundExitProgram`/`BoundGoBack`); the lowerer consults both.

**Lesson (reinforces the CBL3128 sweep lesson):** each wrong heuristic *passed the single target test* (SQ212A)
— only the full guard's breadth exposed that it had over-fit and broken the family. Verify a control-flow change
against the whole suite, never just the one test it was written for.

SQ212A now produces a clean 47-line report — every paragraph PASS, the `FILE CREATED, RECS = 2031` info line,
one footer "017 OF 017 … NO TEST(S) FAILED" — verified non-vacuous and baselined. Guard ALL GREEN: **1040 unit
/ 347 integration / 350 NIST** (SQ 82→83), 0 regressions.

