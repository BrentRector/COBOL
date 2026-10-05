      *> kb/Work PB434 - ISO/IEC 1989:2023 14.9.28.4 GR16 bans a transfer of control OUT of an exception-
      *> checking PERFORM from its FINALLY phrase (imperative-statement-5): "There shall be no statements that
      *> include a transfer of control out of the PERFORM statement within imperative-statement-5"
      *> (negative pb434-finally-transfer-out, COBOLNET2927). This golden pins what the ban does NOT reach:
      *>   - EXIT PERFORM in imperative-statement-5: GR16 "Any EXIT PERFORM statement within
      *>     imperative-statement-5 transfers control to an implicit CONTINUE statement following the
      *>     END-PERFORM" - so the rest of FINALLY is skipped and the statement after END-PERFORM runs;
      *>   - an inline PERFORM nested in FINALLY whose EXIT PERFORM leaves only that inline PERFORM;
      *>   - an out-of-line PERFORM from FINALLY of a paragraph that itself uses GO TO inside its own range
      *>     (the GO TO is not written within imperative-statement-5, and control returns).
      *> EXPECTED OUTPUT, derived line by line:
      *>   IMP1        imperative-statement-1 runs; no exception, so no WHEN phrase
      *>   LOOP 1      the nested inline PERFORM runs once and EXIT PERFORM leaves it
      *>   SUB 2       PERFORM SUB-P THRU SUB-X: SUB-P's GO TO SUB-X stays in the range, N = 2
      *>   FIN-END     still in FINALLY after both
      *>   DONE        EXIT PERFORM in FINALLY: "FIN-SKIPPED" is never displayed
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB434FA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               DISPLAY "IMP1"
           WHEN EC-USER-DEMO
               DISPLAY "WHEN"
           FINALLY
               PERFORM UNTIL N > 0
                   ADD 1 TO N
                   DISPLAY "LOOP " N
                   EXIT PERFORM
               END-PERFORM
               PERFORM SUB-P THRU SUB-X
               DISPLAY "SUB " N
               DISPLAY "FIN-END"
               EXIT PERFORM
               DISPLAY "FIN-SKIPPED"
           END-PERFORM
           DISPLAY "DONE"
           STOP RUN.
       SUB-P.
           ADD 1 TO N
           GO TO SUB-X.
       SUB-Y.
           ADD 5 TO N.
       SUB-X.
           EXIT.
