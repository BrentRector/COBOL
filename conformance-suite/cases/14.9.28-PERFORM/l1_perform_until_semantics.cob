      *>--- conformance-case
      *> id: l1_perform_until_semantics
      *> rules: [GR-14.9.28.4-10]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.28.4 GR10 — the until-phrase's four sentences, one
      *> leg each. (FORMATS 1 AND 2.)
      *>
      *> S1 "the specified set of statements is performed until the
      *>    condition specified by the UNTIL phrase is true. When the
      *>    condition is true, control is transferred to the end of the
      *>    PERFORM statement." LOOPN counts up to 3 and the body runs
      *>    exactly 3 times — not 2 (an off-by-one exit) and not 4 (a
      *>    test that ran after the condition already held).
      *>
      *> S2 "If the condition is true when the PERFORM statement is
      *>    entered, and the TEST BEFORE phrase is specified or
      *>    implied, no transfer to the specified set of statements
      *>    takes place, and control is passed to the end of the
      *>    PERFORM statement." GUARD is 1 and nothing changes it, so
      *>    the body must run ZERO times — asserted inline AND
      *>    out-of-line, the two carriers of the same phrase.
      *>
      *> S3 "If the TEST AFTER phrase is specified, the PERFORM
      *>    statement functions as if the TEST BEFORE phrase were
      *>    specified except that the condition is tested after the
      *>    specified set of statements has been executed." Same
      *>    already-true condition, so the body must run EXACTLY ONCE —
      *>    again on both carriers. S2 and S3 together are what make
      *>    each other non-vacuous.
      *>
      *> S4 "Item identification associated with the operands specified
      *>    in condition-1 is done each time the condition is tested."
      *>    The condition is WE (WK) = 9 and the BODY advances WK, so
      *>    a subscript resolved once at entry would read WE (1) = 0
      *>    for ever and never terminate. WE (3) is the only 9, so the
      *>    identification-per-test reading gives exactly 2 passes and
      *>    leaves WK at 3.
      *>
      *> The rule is worded identically in COBOL-85/2002/2014/2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PFG10.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GUARD  PIC 9 VALUE 1.
       01 LOOPN  PIC 9 VALUE 0.
       01 A-CNT  PIC 9 VALUE 0.
       01 B-CNT  PIC 9 VALUE 0.
       01 C-CNT  PIC 9 VALUE 0.
       01 D-CNT  PIC 9 VALUE 0.
       01 E-CNT  PIC 9 VALUE 0.
       01 F-CNT  PIC 9 VALUE 0.
       01 T-TAB.
          05 WE PIC 9 OCCURS 5 TIMES.
       01 WK     PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
      *> S1 — the loop stops the first time the condition is true.
           PERFORM UNTIL LOOPN = 3
               ADD 1 TO A-CNT
               ADD 1 TO LOOPN
           END-PERFORM.
           DISPLAY "GR10-S1 COUNT=" A-CNT " LOOPN=" LOOPN.
      *> S2 — condition already true at entry, TEST BEFORE implied.
           PERFORM UNTIL GUARD = 1
               ADD 1 TO B-CNT
           END-PERFORM.
           PERFORM BUMP-C UNTIL GUARD = 1.
           DISPLAY "GR10-S2 INLINE=" B-CNT " OUTLINE=" C-CNT.
      *> S3 — the same condition with TEST AFTER: tested AFTER the set
      *> of statements has been executed, so exactly one execution.
           PERFORM WITH TEST AFTER UNTIL GUARD = 1
               ADD 1 TO D-CNT
           END-PERFORM.
           PERFORM BUMP-E WITH TEST AFTER UNTIL GUARD = 1.
           DISPLAY "GR10-S3 INLINE=" D-CNT " OUTLINE=" E-CNT.
      *> S4 — item identification is redone at every test. WE (3) is
      *> the only element holding 9.
           MOVE 0 TO WE (1).
           MOVE 0 TO WE (2).
           MOVE 9 TO WE (3).
           MOVE 0 TO WE (4).
           MOVE 0 TO WE (5).
           MOVE 1 TO WK.
           PERFORM UNTIL WE (WK) = 9
               ADD 1 TO F-CNT
               ADD 1 TO WK
           END-PERFORM.
           DISPLAY "GR10-S4 COUNT=" F-CNT " WK=" WK.
           STOP RUN.

       BUMP-C.
           ADD 1 TO C-CNT.
       BUMP-E.
           ADD 1 TO E-CNT.
