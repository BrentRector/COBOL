*>--- conformance-case
*> id: exit_perform_cycle
*> rules: [GR-14.9.14.4-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> CA32 (CONFORMANCE-FIX-QUEUE): EXIT PERFORM CYCLE inside an inline PERFORM VARYING passes control to
      *> the implicit CONTINUE preceding END-PERFORM (ISO 14.9.14.4 GR5b), so the VARYING augment + re-test
      *> STILL run. The VARYING augment is emitted as a trailing loop-body statement, so a bare C# continue
      *> would skip it — the buggy pre-fix behavior was an infinite loop (I stuck at 2 forever).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA32.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
               IF I = 2
                   EXIT PERFORM CYCLE
               END-IF
               DISPLAY I
           END-PERFORM
           DISPLAY "DONE"
           STOP RUN.
