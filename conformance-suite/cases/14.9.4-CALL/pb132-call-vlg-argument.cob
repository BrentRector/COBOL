*>--- conformance-case
*> id: pb132-call-vlg-argument
*> rules: [SR-14.9.4.3-12]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR12 (FORMAT 1): identifier-2 shall not reference a variable-length group
      *> (8.5.1.12.1 - a DYNAMIC LENGTH elementary item is subordinate to it). One predicate:
      *> ReferenceResolver.HasVariableLengthSubordinate (kb/Work PB132 reconciled the PB124 second copy).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB132N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          02 D PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN.
           CALL "SUB" USING G
           STOP RUN.
