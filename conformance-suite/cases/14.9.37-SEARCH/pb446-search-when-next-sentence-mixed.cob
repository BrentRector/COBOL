*>--- conformance-case
*> id: pb446-search-when-next-sentence-mixed
*> rules: [FMT-14.9.37.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 §14.9.37.2 Format 1: WHEN condition-1 { imperative-statement-2 | NEXT SENTENCE } - a BRACE, so
*> exactly one alternative is written (§5.2.6.3); NEXT SENTENCE followed by another statement in the same WHEN
*> body is neither. Refused at every edition. COBOLNET2269 (kb/Work PB446).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB446WNSMX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05 E OCCURS 5 ASCENDING KEY K INDEXED BY IX.
              10 K PIC 9(2).
       PROCEDURE DIVISION.
           SET IX TO 1
           SEARCH E AT END DISPLAY "NONE"
              WHEN K (IX) = 03 NEXT SENTENCE DISPLAY "TAIL".
           STOP RUN.
