*>--- conformance-case
*> id: l1_implicit_continue_zero
*> rules: [GR-14.9.9.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.9.4 2 — "Implicit CONTINUE statements shall be processed as if AFTER ZERO SECONDS were
      *> specified."  WHAT THAT MEANS, derived from §14.9.9.4 1 with arithmetic-expression-1 = 0:
      *>   (a) 0 is NOT less than zero, so GR1's a)/b)/c) arm cannot be entered — an implicit CONTINUE may
      *>       never set EC-CONTINUE-LESS-THAN-ZERO;
      *>   (b) GR1's tail applies instead — "execution is suspended for the period of time determined by
      *>       arithmetic-expression-1.  When the time is passed, execution continues with the next executable
      *>       statement" — so control must ARRIVE at the next executable statement of each implicit site.
      *> ⚠ HONEST SCOPE: the DURATION itself is not assertable — a wall-clock assertion is a flake, and an
      *> implicit CONTINUE is a conceptual device that is never written in source, so no program can time one.
      *> (a) and (b) are the whole of the rule's OBSERVABLE content and are what this golden pins.
      *> THE EVIDENCE IS ARMED FIRST.  Leg 1 is an EXPLICIT `CONTINUE AFTER <negative> SECONDS`, which by
      *> §14.9.9.4 1 a/b sets the value to 0 AND, checking being enabled by the >>TURN directive above, sets
      *> EC-CONTINUE-LESS-THAN-ZERO — reported by FUNCTION EXCEPTION-STATUS as a "31-character, left-justified,
      *> alphanumeric character string that is the exception-name" (§15.33.3 1), 26 name characters and 5
      *> spaces.  Leg 2 clears it, and §15.33.3 1's last sentence fixes the cleared answer: "If the last
      *> exception status indicates no exception, alphanumeric spaces are returned."  Only then do the implicit
      *> sites run.  The closing silence is therefore a MEASUREMENT taken with a facility proven to answer, not
      *> the quiet of an unarmed gate.
      *> THE IMPLICIT SITES, each named by the clause that creates it:
      *>   §14.9.19.4 4 — NEXT SENTENCE "control is transferred to an implicit CONTINUE statement immediately
      *>     preceding the next separator period" (an archaic form, Annex F.1 — still conforming at 2023).
      *>   §14.9.14.4 5 a — EXIT PERFORM "causes control to be passed to an implicit CONTINUE statement
      *>     immediately following the END-PERFORM phrase".
      *>   §14.9.14.4 6 — EXIT PARAGRAPH "causes control to be passed to an implicit CONTINUE statement
      *>     immediately following the last explicit statement of the current paragraph".
      *> Each site prints a marker on the far side of it, which is (b); the closing EXCEPTION-STATUS is (a).
      >>TURN EC-CONTINUE-LESS-THAN-ZERO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CTI01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L1C-NEG PIC S9V9 VALUE -0.5.
       01 L1C-I   PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           CONTINUE AFTER L1C-NEG SECONDS.
           DISPLAY "ARMED  =[" FUNCTION EXCEPTION-STATUS "]".
           SET LAST EXCEPTION TO OFF.
           DISPLAY "CLEARED=[" FUNCTION EXCEPTION-STATUS "]".
           IF L1C-I = 0 THEN NEXT SENTENCE ELSE DISPLAY "NEXTSNT=ELSE-TAKEN".
           DISPLAY "NEXTSNT=REACHED".
           PERFORM UNTIL L1C-I > 9
               ADD 1 TO L1C-I
               IF L1C-I = 3 EXIT PERFORM END-IF
           END-PERFORM.
           DISPLAY "EXITPFM=" L1C-I.
           PERFORM L1C-EXITP.
           DISPLAY "SILENT =[" FUNCTION EXCEPTION-STATUS "]".
           STOP RUN.
       L1C-EXITP.
           DISPLAY "EXITPAR=BEFORE".
           EXIT PARAGRAPH.
           DISPLAY "EXITPAR=NOT-REACHED".
