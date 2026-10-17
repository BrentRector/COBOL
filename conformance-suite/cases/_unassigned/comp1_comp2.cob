*>--- conformance-case
*> id: comp1_comp2
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. COMP12.
      *> COMP-1/COMP-2 are vendor synonyms of FLOAT-SHORT/FLOAT-LONG (D16);
      *> this also proves the pre-D16 (long)-truncation bug is fixed.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C1 COMP-1.
       01 WS-C2 COMP-2.
       01 WS-R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 10.5 TO WS-C1.
           COMPUTE WS-R = WS-C1 * 2.
           DISPLAY "C1=" WS-R.
           MOVE 100.25 TO WS-C2.
           COMPUTE WS-R = WS-C2 * 4.
           DISPLAY "C2=" WS-R.
           STOP RUN.
