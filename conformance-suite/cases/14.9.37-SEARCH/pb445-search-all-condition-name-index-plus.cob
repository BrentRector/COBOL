*>--- conformance-case
*> id: pb445-search-all-condition-name-index-plus
*> rules: [SR-14.9.37.3-9]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB445 - SEARCH ALL Format 2 (ISO 1989:2023 14.9.37.3 SR7-SR13). Before this item all seven of
*> those syntax rules were unenforced: the binder read the table's index names and occurrence count and
*> nothing key-related, and bound each WHEN through the general condition binder as one opaque expression,
*> so neither the ordered key list nor the WHEN's decomposed operands existed for a rule to be written
*> against. Each of these programs compiled clean and RAN, at every --std.
*>
*> SR9, last sentence: "The index-name subscript shall not be followed by a '+' or a '-'" - the same
*> prohibition SR8 states for a data-name, written again for a condition-name. Both arms are screened; only
*> fixing one of them is this repository's most reproducible defect shape.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB445SEARCHALLCONDITIONNAMEIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 5 TIMES
               ASCENDING KEY IS K
               INDEXED BY IX.
             10 K  PIC 9(2).
                88 K-IS-FIVE VALUE 05.
             10 V  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH ALL E
               AT END DISPLAY "NONE"
               WHEN K-IS-FIVE (IX + 1)
                   DISPLAY "HIT"
           END-SEARCH.
           STOP RUN.
