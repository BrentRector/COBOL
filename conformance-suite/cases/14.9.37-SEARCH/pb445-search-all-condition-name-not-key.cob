*>--- conformance-case
*> id: pb445-search-all-condition-name-not-key
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
*> SR9: "The data-name associated with each condition-name shall be specified in the KEY phrase in the OCCURS
*> clause associated with identifier-1." V-IS-CCC is defined on V, which the KEY phrase does not name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB445SEARCHALLCONDITIONNAMENO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 5 TIMES
               ASCENDING KEY IS K
               INDEXED BY IX.
             10 K  PIC 9(2).
             10 V  PIC X(3).
                88 V-IS-CCC VALUE "ccc".
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH ALL E
               AT END DISPLAY "NONE"
               WHEN V-IS-CCC (IX)
                   DISPLAY "HIT"
           END-SEARCH.
           STOP RUN.
