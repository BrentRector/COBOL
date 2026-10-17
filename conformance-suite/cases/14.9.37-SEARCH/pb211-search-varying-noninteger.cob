*>--- conformance-case
*> id: pb211-search-varying-noninteger
*> rules: [SR-14.9.37.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.37.3 SR5: "Identifier-2 shall reference a data item whose
      *> usage is index or a data item that is an integer." PIC 9(2)V9 is
      *> neither.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB211N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 3 INDEXED BY IX.
             10 K PIC 9.
       01 V PIC 9(2)V9.
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX TO 1.
           SEARCH E VARYING V AT END DISPLAY "NONE"
               WHEN K (IX) = 2 DISPLAY "HIT"
           END-SEARCH.
           STOP RUN.
