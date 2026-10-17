*>--- conformance-case
*> id: pb445-search-all-when-not-a-key
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
*> SR8: data-name-1 and all repetitions of data-name-2 "shall be referenced in the KEY phrase in the OCCURS
*> clause associated with identifier-1". V is an element of E but is not in its KEY phrase, so the WHEN tests
*> a column the table is not ordered on and the ordered search has nothing to converge on.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB445SEARCHALLWHENNOTAKEY.
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
               WHEN V (IX) = "ccc"
                   DISPLAY "HIT"
           END-SEARCH.
           STOP RUN.
