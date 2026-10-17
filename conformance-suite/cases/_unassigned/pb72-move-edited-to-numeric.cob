*>--- conformance-case
*> id: pb72-move-edited-to-numeric
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.25.3 SR10, Table 16: the ALPHANUMERIC-EDITED row's Numeric and
      *> Numeric-edited columns are "No". The de-editing MOVE is the
      *> NUMERIC-EDITED row's (numeric-edited into numeric is "Yes") - an
      *> alphanumeric edit mask has no de-editable numeric value, which is why
      *> the two rows differ and why the sender's edited-ness must be carried.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB72NEGEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-AE PIC XXBXX VALUE "AB CD".
       01 WS-9  PIC 9(3)  VALUE 0.
       PROCEDURE DIVISION.
           MOVE WS-AE TO WS-9
           STOP RUN.
