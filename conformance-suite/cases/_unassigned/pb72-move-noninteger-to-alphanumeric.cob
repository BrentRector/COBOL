*>--- conformance-case
*> id: pb72-move-noninteger-to-alphanumeric
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.25.3 SR10, Table 16: the Numeric-NONINTEGER row admits ONLY the
      *> National and Numeric-family columns - the Alphabetic, Alphanumeric and
      *> Alphanumeric-edited columns are all "No" (the INTEGER row's alphanumeric
      *> "Yes" is the classic digit-image move; a scaled value has no
      *> whole-character digit image to move). Before PB72 MOVE 5.5 TO a PIC X
      *> item was silently accepted and printed "5.5".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB72NEGNX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X(6) VALUE SPACES.
       PROCEDURE DIVISION.
           MOVE 5.5 TO WS-X
           STOP RUN.
