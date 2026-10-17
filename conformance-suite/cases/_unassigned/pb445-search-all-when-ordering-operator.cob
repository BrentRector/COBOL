*>--- conformance-case
*> id: pb445-search-all-when-ordering-operator
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB445 - SEARCH ALL Format 2 (ISO 1989:2023 14.9.37.3 SR7-SR13). Before this item all seven of
*> those syntax rules were unenforced: the binder read the table's index names and occurrence count and
*> nothing key-related, and bound each WHEN through the general condition binder as one opaque expression,
*> so neither the ordered key list nor the WHEN's decomposed operands existed for a rule to be written
*> against. Each of these programs compiled clean and RAN, at every --std.
*>
*> NOT a syntax rule - the GENERAL FORMAT. 14.9.37.2 Format 2 prints the WHEN operand as
*> `data-name-1 { IS EQUAL TO | IS = } { identifier-3 | literal-1 | arithmetic-expression-1 }` or a bare
*> condition-name-1, and prints no other relational operator; an ordering comparison has no meaning for a
*> search that converges on an equal key. It draws the EXISTING COBOLNET1757 ("an operand a statement's own
*> syntax rules or general format do not admit"), not one of PB445's three new codes: the shape question and
*> the syntax rules are different questions and the diagnostic identity follows the mechanism.
*> The serial SEARCH (Format 1) is the form whose WHEN takes any conditional expression (14.9.37.3 SR6).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB445SEARCHALLWHENORDERINGOPE.
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
               WHEN K (IX) > 04
                   DISPLAY "HIT"
           END-SEARCH.
           STOP RUN.
