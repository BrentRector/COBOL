*>--- conformance-case
*> id: pb72-move-numeric-to-alphabetic
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.25.3 SR10, Table 16: the Numeric-Integer row's ALPHABETIC column
      *> is "No" - a numeric sending operand does not move to an alphabetic
      *> receiver (only the alphabetic and alphanumeric families do). Before PB72
      *> the alphabetic COLUMN had no arm at all and MOVE 5 TO an A-item was
      *> silently accepted, storing "5   " into an item whose PICTURE admits only
      *> letters and space.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB72NEGNA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC A(4) VALUE "ABCD".
       PROCEDURE DIVISION.
           MOVE 5 TO WS-A
           STOP RUN.
