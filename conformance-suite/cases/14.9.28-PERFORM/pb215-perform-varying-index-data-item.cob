*>--- conformance-case
*> id: pb215-perform-varying-index-data-item
*> rules: [SR-14.9.28.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB215. ISO 13.18.60.3 SR10: "An index data item may be referenced
      *> explicitly only in a SEARCH or SET statement, a relation condition, an
      *> intrinsic function argument" ... - no PERFORM entry. 13.18.38.3 r7 lists
      *> "the VARYING phrase of a PERFORM statement" for an index-NAME only, and
      *> 14.9.28.3 SR2: "Each identifier shall reference a numeric elementary
      *> item". One operand context served both lists, so FROM/BY IDX computed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB215N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IDX USAGE INDEX.
       01 V PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           SET IDX TO 2
           PERFORM VARYING V FROM IDX BY 1 UNTIL V > 6
               DISPLAY "V=" V
           END-PERFORM
           STOP RUN.
