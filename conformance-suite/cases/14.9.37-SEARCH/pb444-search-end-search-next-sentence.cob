*>--- conformance-case
*> id: pb444-search-end-search-next-sentence
*> rules: [SR-14.9.37.3-4]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 §14.9.37.3 SR4 (ALL FORMATS): "If the END-SEARCH phrase is specified, the NEXT SENTENCE phrase
*> shall not be specified." Format 1 arm. The pair used to compile and be given a MEANING - NEXT SENTENCE jumped
*> past END-SEARCH to the separator period - with no diagnostic below 2023 and only the unrelated archaic-feature
*> warning at 2023. Every edition refuses: the rule is unchanged since 1985. COBOLNET2409 (kb/Work PB444).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB444ESNS1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05 E OCCURS 5 ASCENDING KEY K INDEXED BY IX.
              10 K PIC 9(2).
       PROCEDURE DIVISION.
           SET IX TO 1
           SEARCH E AT END DISPLAY "NONE"
              WHEN K (IX) = 03 NEXT SENTENCE
           END-SEARCH
           DISPLAY "TAIL".
           STOP RUN.
