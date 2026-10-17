*>--- conformance-case
*> id: pb445-search-all-no-key-phrase
*> rules: [SR-14.9.37.3-7]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB445 - SEARCH ALL Format 2 (ISO 1989:2023 14.9.37.3 SR7-SR13). Before this item all seven of
*> those syntax rules were unenforced: the binder read the table's index names and occurrence count and
*> nothing key-related, and bound each WHEN through the general condition binder as one opaque expression,
*> so neither the ordered key list nor the WHEN's decomposed operands existed for a rule to be written
*> against. Each of these programs compiled clean and RAN, at every --std.
*>
*> SR7: "The OCCURS clause associated with identifier-1 shall contain the KEY phrase." E's OCCURS clause
*> declares INDEXED BY but no ASCENDING/DESCENDING KEY, so the statement has no key to compare and the
*> 14.9.37.4 GR5 a) sequencing precondition the form rests on cannot even be stated. The serial SEARCH
*> (Format 1) is the form that needs no KEY phrase; its WHEN takes any conditional expression (SR6).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB445SEARCHALLNOKEYPHRASE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 5 TIMES INDEXED BY IX.
             10 K  PIC 9(2).
             10 V  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH ALL E
               AT END DISPLAY "NONE"
               WHEN K (IX) = 05
                   DISPLAY "HIT"
           END-SEARCH.
           STOP RUN.
