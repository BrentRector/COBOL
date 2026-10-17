*>--- conformance-case
*> id: pb446-search-at-end-next-sentence
*> rules: [FMT-14.9.37.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 §14.9.37.2 (both formats): [ AT END imperative-statement-1 ] - NEXT SENTENCE is not a statement
*> but a phrase, printed in exactly three places: IF Format 2's THEN/ELSE (§14.9.19.2) and the WHEN body of both
*> SEARCH formats. An AT END NEXT SENTENCE used to compile. Refused at every edition. COBOLNET2269 (kb/Work PB446).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB446AENS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05 E OCCURS 5 ASCENDING KEY K INDEXED BY IX.
              10 K PIC 9(2).
       PROCEDURE DIVISION.
           SET IX TO 1
           SEARCH E AT END NEXT SENTENCE
              WHEN K (IX) = 09 DISPLAY "HIT".
           DISPLAY "TAIL".
           STOP RUN.
