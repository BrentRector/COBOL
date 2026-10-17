*>--- conformance-case
*> id: pb2586-unstring-alphabetic-delimiter
*> rules: [SR-14.9.48.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.48.3 SR2 (kb/Work PB2586): identifier-2 and identifier-3 (the DELIMITED BY identifiers) shall
      *> reference data items of category alphanumeric or national; a PIC A delimiter is category alphabetic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2586N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(5) VALUE "AB CD".
       01 D PIC A VALUE SPACE.
       01 B PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           UNSTRING A DELIMITED BY D INTO B
           STOP RUN.
