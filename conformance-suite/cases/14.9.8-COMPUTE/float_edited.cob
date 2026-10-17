*>--- conformance-case
*> id: float_edited
*> rules: [GR-14.9.8.4-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FLTEDT.
      *> a float (COMP-2) source edited into a numeric-edited receiver,
      *> by MOVE and by COMPUTE (ISO §14.9.25.4 GR5 / §14.7.7). D16 review.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-F USAGE COMP-2.
       01 WS-E PIC ZZ9.99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 123.45 TO WS-F.
           MOVE WS-F TO WS-E.
           DISPLAY "E=" WS-E.
           COMPUTE WS-E = WS-F + 1.
           DISPLAY "E2=" WS-E.
           STOP RUN.
