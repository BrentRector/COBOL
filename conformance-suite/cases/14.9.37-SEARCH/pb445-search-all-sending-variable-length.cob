*>--- conformance-case
*> id: pb445-search-all-sending-variable-length
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
*> SR12's identifier-3 arm, the twin of pb445-search-all-key-variable-length. VL is a variable-length group
*> by 8.5.1.12.1 (a subordinate DYNAMIC LENGTH elementary item, 13.18.19).
*>
*> NOTE for anyone re-witnessing this rule: an OCCURS DEPENDING group is NOT a variable-length group.
*> 8.5.1.12.1 names only "dynamic-length elementary item or dynamic-capacity table", and a dynamic-capacity
*> table is OCCURS DYNAMIC (13.18.38 Format 4), not OCCURS ... DEPENDING ON. PB445's own SR12 probe used an
*> ODO group and so measured no violation at all.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB445SEARCHALLSENDINGVARIABLE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 5 TIMES
               ASCENDING KEY IS K
               INDEXED BY IX.
             10 K  PIC X(3).
             10 V  PIC X(3).
       01 VL.
          05 VD PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH ALL E
               AT END DISPLAY "NONE"
               WHEN K (IX) = VL
                   DISPLAY "HIT"
           END-SEARCH.
           STOP RUN.
