*>--- conformance-case
*> id: pb445-search-all-when-literal-subscript
*> rules: [SR-14.9.37.3-8]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB445 - SEARCH ALL Format 2 (ISO 1989:2023 14.9.37.3 SR7-SR13). Before this item all seven of
*> those syntax rules were unenforced: the binder read the table's index names and occurrence count and
*> nothing key-related, and bound each WHEN through the general condition binder as one opaque expression,
*> so neither the ordered key list nor the WHEN's decomposed operands existed for a rule to be written
*> against. Each of these programs compiled clean and RAN, at every --std.
*>
*> SR8: data-name-1 "shall be subscripted by the first index-name associated with identifier-1". K (1) names
*> occurrence ONE for every probe, so the condition does not vary with the search and the statement either
*> matches every occurrence or none.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB445SEARCHALLWHENLITERALSUBS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 5 TIMES
               ASCENDING KEY IS K
               INDEXED BY IX.
             10 K  PIC 9(2).
             10 V  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH ALL E
               AT END DISPLAY "NONE"
               WHEN K (1) = 05
                   DISPLAY "HIT"
           END-SEARCH.
           STOP RUN.
