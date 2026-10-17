*>--- conformance-case
*> id: pb139-accept-vlg
*> rules: [SR-14.9.1.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.1.3 SR6: neither identifier-1 nor identifier-2 shall reference a variable-length group
      *> (8.5.1.12 - a DYNAMIC LENGTH elementary item is subordinate). One predicate:
      *> ReferenceResolver.HasVariableLengthSubordinate (kb/Work PB139).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB139N7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          02 D PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN.
           ACCEPT G
           STOP RUN.
