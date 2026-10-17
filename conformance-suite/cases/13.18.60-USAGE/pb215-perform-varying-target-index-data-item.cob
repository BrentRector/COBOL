*>--- conformance-case
*> id: pb215-perform-varying-target-index-data-item
*> rules: [SR-13.18.60.3-10, SR-14.9.28.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB215, the other arm of the FROM/BY fix. The induction variable
      *> was resolved through SET's receiver, which admits an index data item
      *> because 13.18.60.3 SR10 lists "a SEARCH or SET statement". PERFORM is not
      *> on that list, and 14.9.28.3 SR2: "Each identifier shall reference a
      *> numeric elementary item". It varied silently.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB215N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IDX USAGE INDEX.
       01 N PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM VARYING IDX FROM 1 BY 1 UNTIL IDX > 3
               ADD 1 TO N
           END-PERFORM
           DISPLAY "N=" N
           STOP RUN.
