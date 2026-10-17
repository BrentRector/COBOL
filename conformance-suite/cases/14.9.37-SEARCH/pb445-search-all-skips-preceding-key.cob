*>--- conformance-case
*> id: pb445-search-all-skips-preceding-key
*> rules: [SR-14.9.37.3-11]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB445 - SEARCH ALL Format 2 (ISO 1989:2023 14.9.37.3 SR7-SR13). Before this item all seven of
*> those syntax rules were unenforced: the binder read the table's index names and occurrence count and
*> nothing key-related, and bound each WHEN through the general condition binder as one opaque expression,
*> so neither the ordered key list nor the WHEN's decomposed operands existed for a rule to be written
*> against. Each of these programs compiled clean and RAN, at every --std.
*>
*> SR11: "When a data-name in the KEY phrase ... is referenced ... all preceding data-names in that KEY
*> phrase or their associated condition-names shall also be referenced." The WHEN references K2 and not K,
*> and 13.18.38.4 GR3 makes the phrase's order its order of significance - a table ordered on (K, K2) is not
*> ordered on K2 alone, so a binary search on K2 may miss a present element entirely.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB445SEARCHALLSKIPSPRECEDINGK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 5 TIMES
               ASCENDING KEY IS K K2
               INDEXED BY IX.
             10 K  PIC 9(2).
             10 K2 PIC 9(2).
             10 V  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH ALL E
               AT END DISPLAY "NONE"
               WHEN K2 (IX) = 05
                   DISPLAY "HIT"
           END-SEARCH.
           STOP RUN.
