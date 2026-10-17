*>--- conformance-case
*> id: pb655_dropped_words_user_words
*> rules: [SR-8.3.2.1-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.9 - words the 2002 reserved-word list DROPPED are user-defined words
      *> from 2002 on (kb/Work PB655). ALTER, AUTHOR, LABEL, TAPE and SECURITY are
      *> reserved at 85 only (reserved-words.json r85 true, r2002/r2014/r2023 false),
      *> yet each is a lexer token because the 85 formats spell it, and until PB655 no
      *> cobolWord row admitted the token - so every declaration and reference below was
      *> a raw COBOL0001 parse error at 2002, 2014 and 2023.
      *> Legs: a plain name, a SUBSCRIPTED table element (the lexer's subscript-trigger
      *> set must carry the token), a QUALIFIED reference, and each word as a NON-FIRST
      *> operand of a DISPLAY list - where ALTER could begin the next statement. The
      *> token-level reservation gate retypes each DECLARED free word to a plain name
      *> before the parse that counts, so every one of those positions reads a name.
      *> Expected output read off the source: VALUE clauses and the two MOVEs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB655-DROPPED-WORDS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-G.
           05  ALTER         PIC X(2) VALUE "AL".
           05  AUTHOR        PIC X(2) VALUE "AU".
           05  SECURITY      PIC X(2) VALUE "SE".
       01  WS-T.
           05  LABEL         PIC X(2) OCCURS 2.
       01  TAPE              PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "L1" TO LABEL (1)
           MOVE "L2" TO LABEL (TAPE)
           DISPLAY "A=" ALTER " " AUTHOR " " SECURITY
           DISPLAY "Q=" ALTER OF WS-G
           DISPLAY "L=" LABEL (1) LABEL (2) " T=" TAPE
           STOP RUN.
