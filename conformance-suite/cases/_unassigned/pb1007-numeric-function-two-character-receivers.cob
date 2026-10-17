*>--- conformance-case
*> id: pb1007-numeric-function-two-character-receivers
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1007. The multi-receiver MOVE materializes a function-identifier into a 15.4
      *> temporary (14.9.25.4 GR1), and that temporary is flagged to MOVE as the function does - it
      *> must not LEGALIZE what the function itself may not do. NUMVAL is a NUMERIC function (15.2
      *> item 4), the Noninteger row of 14.9.25.3 Table 16, which SR10 refuses into an alphanumeric
      *> receiver; the refusal is decided against the source the programmer wrote, never against the
      *> implementor's intermediate item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1007NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R1 PIC X(8).
       01 R2 PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION NUMVAL("3.7") TO R1 R2
           DISPLAY R1 R2
           STOP RUN.
