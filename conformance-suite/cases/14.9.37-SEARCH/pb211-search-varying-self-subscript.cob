*>--- conformance-case
*> id: pb211-search-varying-self-subscript
*> rules: [SR-14.9.37.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.37.3 SR5, second sentence: identifier-2 "shall not be
      *> subscripted by the first or only index-name specified in the INDEXED
      *> phrase" of identifier-1. K (IX) moves with every step of the scan.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB211N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 3 INDEXED BY IX.
             10 K PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX TO 1.
           SEARCH E VARYING K (IX) AT END DISPLAY "NONE"
               WHEN K (IX) = 2 DISPLAY "HIT"
           END-SEARCH.
           STOP RUN.
