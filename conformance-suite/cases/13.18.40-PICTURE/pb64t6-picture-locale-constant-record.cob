*>--- conformance-case
*> id: pb64t6-picture-locale-constant-record
*> rules: [SR-13.18.40.3-32]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.40.3 SR32 - "A format 2 PICTURE clause shall not be specified in a data item described with
      *> the CONSTANT RECORD clause, or in any data item subordinate to a data item described with the CONSTANT
      *> RECORD clause" - COBOLNET1673 names the rule for the SUBORDINATE half (the parent chain check;
      *> kb/Work PB64 T6).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T6CR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr-FR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CR1 CONSTANT RECORD.
          05 F1 PIC Z9 LOCALE IS FR SIZE IS 4 VALUE 1.
       PROCEDURE DIVISION.
           STOP RUN.
