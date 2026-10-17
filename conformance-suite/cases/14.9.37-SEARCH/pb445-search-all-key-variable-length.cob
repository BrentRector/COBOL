*>--- conformance-case
*> id: pb445-search-all-key-variable-length
*> rules: [SR-14.9.37.3-12]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB445 - SEARCH ALL Format 2 (ISO 1989:2023 14.9.37.3 SR7-SR13). Before this item all seven of
*> those syntax rules were unenforced: the binder read the table's index names and occurrence count and
*> nothing key-related, and bound each WHEN through the general condition binder as one opaque expression,
*> so neither the ordered key list nor the WHEN's decomposed operands existed for a rule to be written
*> against. Each of these programs compiled clean and RAN, at every --std.
*>
*> SR12: "Data-name-1, data-name-2, identifier-3, or identifier-4 shall not specify a variable-length group."
*> G is a variable-length group by 8.5.1.12.1 - "a group item whose data description has at least one
*> dynamic-length elementary item or dynamic-capacity table as a subordinate item". Rejected from 2014, the
*> first edition in which a DYNAMIC LENGTH item (13.18.19) can be declared at all; below it the declaration
*> is itself refused, so there is no edition at which this program is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB445SEARCHALLKEYVARIABLELENG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 5 TIMES
               ASCENDING KEY IS G
               INDEXED BY IX.
             10 G.
                15 VD PIC X DYNAMIC LENGTH.
             10 V  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH ALL E
               AT END DISPLAY "NONE"
               WHEN G (IX) = "a"
                   DISPLAY "HIT"
           END-SEARCH.
           STOP RUN.
