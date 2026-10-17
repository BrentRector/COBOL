*>--- conformance-case
*> id: pb445-search-all-sending-uses-index
*> rules: [SR-14.9.37.3-10]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB445 - SEARCH ALL Format 2 (ISO 1989:2023 14.9.37.3 SR7-SR13). Before this item all seven of
*> those syntax rules were unenforced: the binder read the table's index names and occurrence count and
*> nothing key-related, and bound each WHEN through the general condition binder as one opaque expression,
*> so neither the ordered key list nor the WHEN's decomposed operands existed for a rule to be written
*> against. Each of these programs compiled clean and RAN, at every --std.
*>
*> SR10's SECOND prohibition: the sending operand shall not be "subscripted by the first index-name
*> associated with identifier-1". V is not a key, so only the index half of the rule is violated: the
*> operand moves with the probe instead of being compared against it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB445SEARCHALLSENDINGUSESINDE.
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
               WHEN K (IX) = V (IX)
                   DISPLAY "HIT"
           END-SEARCH.
           STOP RUN.
