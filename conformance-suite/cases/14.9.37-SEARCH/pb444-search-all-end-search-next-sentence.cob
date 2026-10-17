*>--- conformance-case
*> id: pb444-search-all-end-search-next-sentence
*> rules: [SR-14.9.37.3-4]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 §14.9.37.3 SR4 (ALL FORMATS): "If the END-SEARCH phrase is specified, the NEXT SENTENCE phrase
*> shall not be specified." Format 2 arm - the SECOND arm of the same rule (both formats reach one check, so
*> they cannot disagree). Every edition refuses. COBOLNET2409 (kb/Work PB444).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB444ESNS2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05 E OCCURS 5 ASCENDING KEY K INDEXED BY IX.
              10 K PIC 9(2).
       PROCEDURE DIVISION.
           SEARCH ALL E AT END DISPLAY "NONE"
              WHEN K (IX) = 03 NEXT SENTENCE
           END-SEARCH
           DISPLAY "TAIL".
           STOP RUN.
