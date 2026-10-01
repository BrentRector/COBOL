      *> kb/Work PB1160 - a RESUME executed within the scope of
      *> execution of a GLOBAL declarative is a CONTINUE.
      *>
      *> THE RULES.
      *> §14.9.33.4 GR1: "If the RESUME statement is executed within the
      *>   scope of execution of a global declarative, it is the
      *>   equivalent of the execution of a CONTINUE statement."
      *>   OK  §14.9.33.4 1)  (General rules)
      *> §14.9.49.3 SR4: "Procedure-names within a declarative section
      *>   may be referenced in a different declarative section or in a
      *>   nondeclarative procedure only with a PERFORM statement." - so
      *>   the GLOBAL declarative G may PERFORM the local declarative D,
      *>   and D's RESUME is then executed within G's scope.
      *>   OK  §14.9.49.3 4)  (Syntax rules)
      *> §14.9.49.4 GR7 b) with c) and DOC-A.1-218 (docs/CONFORMANCE.md
      *>   §7): after the USE procedure for the failed OPEN (status 35,
      *>   a fatal I-O status) completes, this implementation continues
      *>   at the end of the OPEN statement.
      *>   OK  §14.9.49.4 7) b)  (General rules)
      *>
      *> DERIVATION. Each OPEN of the absent file F fails with status 35
      *> and selects the GLOBAL declarative G (file-name tier, GR3 a)).
      *> G performs D, and D executes a RESUME. Because D runs within G's
      *> scope of execution the RESUME is a CONTINUE, so D falls through
      *> to D-AFTER-RESUME, G falls through to G-END, and control then
      *> reaches the statement after the OPEN. Three arms:
      *>   ARM-N: RESUME AT NEXT STATEMENT  (GR2 would skip the rest of D)
      *>   ARM-P: RESUME AT M2              (GR3 would be a GO TO M2:
      *>                                      M2-JUMPED must never print)
      *>   ARM-C: the OPEN is in a CONTAINED program, so G runs in the
      *>          container's instance on its behalf (GR4 b)); the
      *>          contained program continues after its OPEN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1160GR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "no-such-file-pb1160.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F IS GLOBAL.
       01 FREC PIC X.
       WORKING-STORAGE SECTION.
       01 FS PIC XX GLOBAL.
       01 WS-MODE PIC X VALUE "N".
       PROCEDURE DIVISION.
       DECLARATIVES.
       G SECTION.
           USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON F.
       G1.
           DISPLAY "G-START"
           PERFORM D
           DISPLAY "G-END".
       D SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON OUTPUT.
       D1.
           DISPLAY "D-START"
           IF WS-MODE = "N"
               RESUME AT NEXT STATEMENT
           ELSE
               RESUME AT M2
           END-IF
           DISPLAY "D-AFTER-RESUME".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           DISPLAY "ARM-N"
           OPEN INPUT F
           DISPLAY "MAIN-AFTER-OPEN " FS
           MOVE "P" TO WS-MODE
           DISPLAY "ARM-P"
           OPEN INPUT F
           DISPLAY "MAIN-AFTER-OPEN " FS
           DISPLAY "ARM-C"
           CALL "PB1160IN"
           DISPLAY "MAIN-END"
           STOP RUN.
       M2.
           DISPLAY "M2-JUMPED"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1160IN.
       PROCEDURE DIVISION.
       I1.
           OPEN INPUT F
           DISPLAY "INNER-AFTER-OPEN " FS
           GOBACK.
       END PROGRAM PB1160IN.
       END PROGRAM PB1160GR.
