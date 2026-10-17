*>--- conformance-case
*> id: pb445-search-all-sending-is-key
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
*> SR10: identifier-3 "shall be neither referenced in the KEY phrase of the OCCURS clause associated with
*> identifier-1 nor subscripted by the first index-name associated with identifier-1". K2 is a key of E, so
*> comparing K (IX) against it compares the table with itself. Written K2 (1) deliberately: the subscript is
*> not the search index, so this program isolates SR10's FIRST prohibition from its second.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB445SEARCHALLSENDINGISKEY.
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
               WHEN K (IX) = K2 (1)
                   DISPLAY "HIT"
           END-SEARCH.
           STOP RUN.
