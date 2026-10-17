*>--- conformance-case
*> id: perform_until_exit
*> rules: [GR-14.9.28.4-11]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PERFORM … UNTIL EXIT (ISO §14.9.28.4 GR11, COBOL-2023): an unconditional infinite loop. The inline loop is
      *> escaped by EXIT PERFORM (NOTE 4). Iterates ADD/DISPLAY until WS-N = 3, then EXIT PERFORM → falls to DONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UNTILEXIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM UNTIL EXIT
               ADD 1 TO WS-N
               DISPLAY WS-N
               IF WS-N = 3
                   EXIT PERFORM
               END-IF
           END-PERFORM
           DISPLAY "DONE"
           STOP RUN.
