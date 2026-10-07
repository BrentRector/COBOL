      *> kb/Work PB892 - a condition a USER-DEFINED FUNCTION propagates
      *> (GOBACK RAISING EXCEPTION) is raised in the ACTIVATING element
      *> wherever the reference is written - ISO 14.9.18.4 GR1 b): "an
      *> exception condition is raised in the activating runtime element
      *> if checking for that exception condition is enabled in the
      *> activating runtime element". A RESUME AT NEXT STATEMENT for it
      *> resumes after the STATEMENT the reference was specified in -
      *> 14.9.33.4 GR2 a) 2. "for an inline invocation or a function
      *> invocation, it is the statement in which the inline invocation
      *> or function invocation was specified", and a) 3. "the lowest
      *> level statement, not the containing statement".
      *>
      *> C1 PERFORM UNTIL condition (per evaluation): DECL, then after
      *>    the PERFORM - BODY-1 never shows (the test is before).
      *> C2 COMPUTE (hoisted): DECL, the COMPUTE is abandoned - X stays 5.
      *> C3 COMPUTE, the declarative completes normally (no RESUME): the
      *>    COMPUTE finishes with the function's result 7 - X = 8
      *>    (GR1 b) "execution continues ... as specified in the rules
      *>    for the activating statement after the result ... is
      *>    returned").
      *> C4 IF condition (hoisted): DECL, then after the IF - no THEN.
      *> C5 a non-first AND operand (per evaluation): after the IF.
      *> C6 the same IF nested in an inline PERFORM: the IF is the lowest
      *>    statement, so IN-6 still shows on both iterations.
      *> C7 RESUME AT procedure-name from a PERFORM UNTIL condition:
      *>    control goes to R-SEVEN (GR3), NEVER-7 does not show.
      *> C8 a SEARCH WHEN condition: after the SEARCH.
       >>TURN EC-USER-PBZ EC-USER-PBC CHECKING ON
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB892FZ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R RAISING EC-USER-PBZ.
       FZ-P.
           MOVE 7 TO R.
           GOBACK RAISING EXCEPTION EC-USER-PBZ.
       END FUNCTION PB892FZ.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB892FC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R RAISING EC-USER-PBC.
       FC-P.
           MOVE 7 TO R.
           GOBACK RAISING EXCEPTION EC-USER-PBC.
       END FUNCTION PB892FC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB892M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB892FZ
           FUNCTION PB892FC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 99 VALUE 5.
       01 N PIC 9 VALUE 0.
       01 T.
          05 TE PIC 9 OCCURS 3 INDEXED BY TI.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HZ SECTION. USE AFTER EXCEPTION CONDITION EC-USER-PBZ.
       HZ-P.
           DISPLAY "DECL-Z " N.
           IF N = 7
               RESUME AT R-SEVEN
           END-IF.
           RESUME AT NEXT STATEMENT.
       HC SECTION. USE AFTER EXCEPTION CONDITION EC-USER-PBC.
       HC-P.
           DISPLAY "DECL-C " N.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE 1 TO N.
           PERFORM UNTIL FUNCTION PB892FZ = 7
               DISPLAY "BODY-1"
           END-PERFORM.
           DISPLAY "AFTER-1".
           MOVE 2 TO N.
           COMPUTE X = FUNCTION PB892FZ + 1.
           DISPLAY "X2=" X.
           MOVE 3 TO N.
           COMPUTE X = FUNCTION PB892FC + 1.
           DISPLAY "X3=" X.
           MOVE 4 TO N.
           IF FUNCTION PB892FZ = 7
               DISPLAY "THEN-4"
           END-IF.
           DISPLAY "AFTER-4".
           MOVE 5 TO N.
           IF X = 8 AND FUNCTION PB892FZ = 7
               DISPLAY "THEN-5"
           END-IF.
           DISPLAY "AFTER-5".
           MOVE 6 TO N.
           PERFORM 2 TIMES
               IF X = 8 AND FUNCTION PB892FZ = 7
                   DISPLAY "THEN-6"
               END-IF
               DISPLAY "IN-6"
           END-PERFORM.
           DISPLAY "AFTER-6".
           MOVE 7 TO N.
           PERFORM UNTIL FUNCTION PB892FZ = 7
               DISPLAY "BODY-7"
           END-PERFORM.
           DISPLAY "NEVER-7".
       R-SEVEN.
           DISPLAY "AT-R-SEVEN".
           MOVE 8 TO N.
           MOVE 1 TO TE (1) TE (2) TE (3).
           SET TI TO 1.
           SEARCH TE
               AT END DISPLAY "END-8"
               WHEN FUNCTION PB892FZ = TE (TI) DISPLAY "FOUND-8"
           END-SEARCH.
           DISPLAY "AFTER-8".
           STOP RUN.
       END PROGRAM PB892M.
