*>--- conformance-case
*> id: pb909-search-all-not-at-end
*> rules: [FMT-14.9.37.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 §14.9.37.2 Format 2 (all) prints "SEARCH ALL identifier-1 [ AT END imperative-statement-1 ]"
*> and no NOT AT END phrase - the SECOND arm of the same refusal as Format 1 (the binder has one helper for
*> both, so they cannot disagree). It used to be a COBOLNET1756 deferral and a run-unit abort. §4.2.2;
*> every edition refuses. COBOLNET2269 (kb/Work PB909).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB909NSAE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05 E PIC 9(2) OCCURS 3 ASCENDING KEY E INDEXED BY IX.
       PROCEDURE DIVISION.
           SEARCH ALL E
              AT END DISPLAY "NONE"
              NOT AT END DISPLAY "FOUND"
              WHEN E(IX) = 99 DISPLAY "HIT"
           END-SEARCH
           STOP RUN.
