      *> ISO §14.6.3 3) — explicit/implicit transfer of control: empty procedures, EXIT PROGRAM, implicit GOBACK
      *> RULE (§14.6.3, cite.py OK, the paragraphs of item 3) pinned here):
      *>  (A) "The procedure branching statement EXIT PROGRAM causes an
      *>      explicit transfer of control only when the statement is
      *>      executed in a called program." (with §14.9.14.4 2) "treated
      *>      as if it were a CONTINUE statement", cite.py OK)
      *>  (B) "If control is transferred either implicitly or explicitly to
      *>      a procedure containing no statements, execution proceeds as
      *>      if the procedure contained only a single sentence consisting
      *>      of a CONTINUE statement."
      *>  (C) "There is also no next executable statement after the last
      *>      statement in a source element when the procedure in which it
      *>      appears is not being executed under the control of some other
      *>      COBOL statement ..., and when there are no procedure division
      *>      statements in a program ... In these cases, an implicit
      *>      GOBACK statement without any optional phrases is executed."
      *>  The fatal-declarative sentence ends the run unit abnormally (a
      *>  nonzero exit, which a corpus golden cannot carry); it is pinned by
      *>  the companion test TransferOfControlFatalDeclarativeTests.
      *> DERIVATION (every line is a checkpoint):
      *>  M1                  first statement
      *>  M2                  PERFORM P5, P5 is EMPTY: (B) -> CONTINUE, the
      *>                      PERFORM completes and control returns
      *>  M3                  PERFORM S3, S3 is an EMPTY section: (B)
      *>  SUBX / M4           CALL L1TOCSX; it falls off its last
      *>                      statement: (C) implicit GOBACK -> returns
      *>  M5                  CALL L1TOCSE, a program with NO procedure
      *>                      division statements: (C) implicit GOBACK
      *>  SUBP-1 / M6         CALL L1TOCSP; its EXIT PROGRAM executes in a
      *>                      CALLED program: (A) transfers control back,
      *>                      so SUBP-NOT never prints
      *>  BEFORE-EXIT
      *>  AFTER-EXIT          EXIT PROGRAM in the MAIN program (not
      *>                      called): (A) no transfer, CONTINUE, so
      *>                      control falls into P3
      *>  S2A                 GO TO P5 (empty, (B)) -> falls through into
      *>                      section S2
      *>  S4A                 S3 is empty ((B)), falls into S4
      *>  (end)               the last statement of the main program is not
      *>                      under the control of another statement: (C)
      *>                      implicit GOBACK ends the run unit normally
      *>                      (exit code 0 required by the corpus).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TOCMN.
       PROCEDURE DIVISION.
       S1 SECTION.
       P1.
           DISPLAY "M1".
           PERFORM P5.
           DISPLAY "M2".
           PERFORM S3.
           DISPLAY "M3".
           CALL "L1TOCSX".
           DISPLAY "M4".
           CALL "L1TOCSE".
           DISPLAY "M5".
           CALL "L1TOCSP".
           DISPLAY "M6".
       P2.
           DISPLAY "BEFORE-EXIT".
           EXIT PROGRAM.
       P3.
           DISPLAY "AFTER-EXIT".
           GO TO P5.
       P5.
       S2 SECTION.
       S2A.
           DISPLAY "S2A".
       S3 SECTION.
       S4 SECTION.
       S4A.
           DISPLAY "S4A".
       END PROGRAM L1TOCMN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TOCSX.
       PROCEDURE DIVISION.
       SX1.
           DISPLAY "SUBX".
       END PROGRAM L1TOCSX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TOCSE.
       PROCEDURE DIVISION.
       END PROGRAM L1TOCSE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TOCSP.
       PROCEDURE DIVISION.
       SP1.
           DISPLAY "SUBP-1".
           EXIT PROGRAM.
       SP2.
           DISPLAY "SUBP-NOT".
       END PROGRAM L1TOCSP.
