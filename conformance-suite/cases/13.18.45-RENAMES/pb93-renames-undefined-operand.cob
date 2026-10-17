*>--- conformance-case
*> id: pb93-renames-undefined-operand
*> rules: [SR-13.18.45.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.45.3 SR4: RENAMES data-name-2 and data-name-3 "shall be names of elementary items or groups of
      *> elementary items in the same record" - an operand naming nothing was skipped silently (kb/Work PB93).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB93N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC.
          05 A PIC X(2) VALUE "ab".
          05 B PIC X(2) VALUE "cd".
       66 AB RENAMES A THRU NOPE.
       PROCEDURE DIVISION.
           DISPLAY AB.
           STOP RUN.
