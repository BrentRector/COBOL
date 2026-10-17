*>--- conformance-case
*> id: pb445-search-all-zero-length-literal
*> rules: [SR-14.9.37.3-13]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB445 - SEARCH ALL Format 2 (ISO 1989:2023 14.9.37.3 SR7-SR13). Before this item all seven of
*> those syntax rules were unenforced: the binder read the table's index names and occurrence count and
*> nothing key-related, and bound each WHEN through the general condition binder as one opaque expression,
*> so neither the ordered key list nor the WHEN's decomposed operands existed for a rule to be written
*> against. Each of these programs compiled clean and RAN, at every --std.
*>
*> SR13: "Neither literal-1 nor literal-2 shall be zero-length literals." 3.178 defines a zero-length literal
*> as an "alphanumeric, boolean, or national literal that contains zero characters", and 8.3.3.1 makes
*> contiguous delimiters the spelling of one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB445SEARCHALLZEROLENGTHLITER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 5 TIMES
               ASCENDING KEY IS V
               INDEXED BY IX.
             10 K  PIC 9(2).
             10 V  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH ALL E
               AT END DISPLAY "NONE"
               WHEN V (IX) = ""
                   DISPLAY "HIT"
           END-SEARCH.
           STOP RUN.
