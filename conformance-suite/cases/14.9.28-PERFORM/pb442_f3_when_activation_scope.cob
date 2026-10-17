*>--- conformance-case
*> id: pb442_f3_when_activation_scope
*> rules: [GR-14.9.28.4-1, GR-14.9.28.4-17]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB442 - the scope of an exception-checking PERFORM's WHEN phrase: DYNAMIC within the
      *> activation, and bounded by it (docs/CONFORMANCE.md §3 D-F3S).
      *>
      *> THE RULES, and every expected value below derived from them (no observation):
      *>  14.9.28.4 GR17 - "If during the execution of imperative-statement-1 an exception condition
      *>    associated with a WHEN phrase is raised", imperative-statement-2 is executed; GR1 puts every
      *>    statement executed as a result of a transfer of control in the range of the PERFORM (Q1).
      *>  14.9.28.4 GR20 - a nonfatal condition continues "with an implicit CONTINUE statement immediately
      *>    following the statement in imperative-statement-1 in which the exception condition occurred":
      *>    in Q1 the RAISE in Q1-SUB (the statement that raised it, as 14.9.33.4 GR2 a) 1. says for RESUME
      *>    NEXT STATEMENT), so Q1-SUB-AFTER follows Q1-WHEN; in Q3 the CALL statement.
      *>  14.6.13.1.1 - an exception status indicator "exists for each function, method, or program", so a
      *>    condition raised in the CALLED program is processed there (14.6.13.1.4: no conditional phrase,
      *>    no PERFORM of ITS OWN whose imperative-statement-1 is executing, no USE statement, so 4)
      *>    execution continues). The caller's WHEN never runs for it (Q2), even though the callee holds an
      *>    exception-checking PERFORM of its own.
      *>  14.9.18.4 GR1 b) - GOBACK ... RAISING raises the condition "in the activating runtime element if
      *>    checking for that exception condition is enabled in the activating runtime element" - here by
      *>    GR14's implicit TURN and the leading >>TURN - at the CALL statement (14.9.33.4 GR2 a) 2.), which
      *>    is in imperative-statement-1, so the caller's WHEN runs (Q3). EC-USER conditions are nonfatal
      *>    (14.6.13.1.1), so execution continues after the CALL.
      *>
      *> WHY EACH LEG CAN FAIL: Q1 fails if the WHEN reached only statements written inside the PERFORM; Q2
      *> fails if the caller's WHEN runs inside the callee (it did, before kb/Work PB442, whenever the
      *> callee contained an exception-checking PERFORM); Q3 fails if propagation skipped the activator's
      *> WHEN or resumed anywhere but after the CALL.
      *>
      *> EDITIONS: 2023 only - the exception-checking PERFORM is a 2023 facility; the edition gate is
      *> negative/pb441-f3-perform-below-2023.
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB442MAIN.
       PROCEDURE DIVISION.
       M-Q1.
           PERFORM
               DISPLAY "Q1-HEAD"
               PERFORM Q1-SUB
               DISPLAY "Q1-TAIL"
           WHEN EC-USER-PBFOURA
               DISPLAY "Q1-WHEN"
           END-PERFORM.
       M-Q2.
           PERFORM
               CALL "PB442CEE"
               DISPLAY "Q2-AFTER-CALL"
           WHEN EC-USER-PBFOURA
               DISPLAY "Q2-WHEN-WRONG"
           END-PERFORM.
       M-Q3.
           PERFORM
               CALL "PB442RSE"
               DISPLAY "Q3-AFTER-CALL"
           WHEN EC-USER-PBFOURA
               DISPLAY "Q3-WHEN"
           END-PERFORM
           DISPLAY "DONE"
           STOP RUN.
       Q1-SUB.
           DISPLAY "Q1-SUB"
           RAISE EXCEPTION EC-USER-PBFOURA
           DISPLAY "Q1-SUB-AFTER".
       END PROGRAM PB442MAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB442CEE.
       PROCEDURE DIVISION.
       C-MAIN.
           DISPLAY "CEE-RAISE"
           RAISE EXCEPTION EC-USER-PBFOURA
           DISPLAY "CEE-AFTER"
           PERFORM
               DISPLAY "CEE-F3"
           WHEN EC-USER-PBFOURB
               DISPLAY "CEE-WHEN"
           END-PERFORM
           GOBACK.
       END PROGRAM PB442CEE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB442RSE.
       PROCEDURE DIVISION RAISING EC-USER-PBFOURA.
       R-MAIN.
           DISPLAY "RSE-IN"
           GOBACK RAISING EXCEPTION EC-USER-PBFOURA.
       END PROGRAM PB442RSE.
