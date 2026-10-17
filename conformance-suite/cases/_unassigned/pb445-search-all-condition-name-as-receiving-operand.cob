*>--- conformance-case
*> id: pb445-search-all-condition-name-as-receiving-operand
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB445 - SEARCH ALL Format 2 (ISO 1989:2023 14.9.37.2). NOT a syntax rule - the GENERAL FORMAT.
*> Format 2's WHEN operand is a single required brace with TWO alternatives: the
*> `data-name-1 { IS EQUAL TO | IS = } { identifier-3 | literal-1 | arithmetic-expression-1 }` comparison, OR
*> a bare condition-name-1. A condition-name is an ALTERNATIVE TO the whole comparison, never its receiving
*> operand, so `WHEN K-ONE (IX) = 05` is a shape the format does not print.
*>
*> It draws the EXISTING COBOLNET1757 ("an operand a statement's own syntax rules or general format do not
*> admit"), not one of PB445's three syntax-rule codes: the shape question and the syntax rules are different
*> questions and the diagnostic identity follows the mechanism. Without this arm the operand fell through to
*> 14.9.37.3 SR8's "not referenced in the KEY phrase" verdict, which sends a reader looking for a KEY phrase
*> edit that would not help - K-ONE's own data-name K IS the key.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB445SEARCHALLCONDNAMERECV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 5 TIMES
               ASCENDING KEY IS K
               INDEXED BY IX.
             10 K  PIC 9(2).
                88 K-ONE VALUE 05.
             10 V  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH ALL E
               AT END DISPLAY "NONE"
               WHEN K-ONE (IX) = 05
                   DISPLAY "HIT"
           END-SEARCH.
           STOP RUN.
