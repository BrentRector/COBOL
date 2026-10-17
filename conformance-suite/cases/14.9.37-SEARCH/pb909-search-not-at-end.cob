*>--- conformance-case
*> id: pb909-search-not-at-end
*> rules: [FMT-14.9.37.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 §14.9.37.2 Format 1 (serial) prints "[ AT END imperative-statement-1 ]" and no NOT AT END
*> phrase. The grammar's shared at-end clause admits the vendor NOT AT END branch, which used to bind to the
*> DEFERRAL carrier: a COBOLNET1756 warning, then a run-unit abort when the SEARCH ran. §4.2.2 makes the
*> compile-time indication of a general-format violation mandatory; every edition refuses. COBOLNET2269
*> (kb/Work PB909).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB909NSNE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05 E PIC 9(2) OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION.
           SET IX TO 1
           SEARCH E
              AT END DISPLAY "NONE"
              NOT AT END DISPLAY "FOUND"
              WHEN E(IX) = 99 DISPLAY "HIT"
           END-SEARCH
           STOP RUN.
