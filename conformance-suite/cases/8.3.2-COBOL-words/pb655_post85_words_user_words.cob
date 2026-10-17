      *>--- conformance-case
      *> id: pb655_post85_words_user_words
      *> rules: [SR-8.3.2.1-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 8.9 + 8.3.2.1 1) - words the 2002 reserved-word list ADDED are ordinary
      *> user-defined words in COBOL-85 (kb/Work PB655). GOBACK, NULL, TYPEDEF, SELF,
      *> OBJECT and RETURNING are r85 false in reserved-words.json, yet each is a lexer
      *> token because the 2002 formats spell it; every use below is legal COBOL-85.
      *> Legs, one per way a name can reach the parser:
      *>  - data-names declared at level 01 (the reservedGatedWord definition slot);
      *>  - an INDEX-NAME (INDEXED BY OBJECT) and a SPECIAL-NAMES CLASS-NAME
      *>    (CLASS RETURNING) - slots that do not offer reservedGatedWord, freed by the
      *>    syntax-error witness of the token-level gate;
      *>  - NULL and GOBACK as NON-FIRST DISPLAY operands, where each is also the
      *>    leading keyword of a 2002 construct;
      *>  - GOBACK inside an arithmetic SUBSCRIPT (SELF (GOBACK + 1)), which the binder
      *>    re-parses as a fragment - the fragment must read the word as the tree does.
      *> Expected output read off the VALUE clauses, the MOVEs and the CLASS clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB655-POST85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS RETURNING IS "A" THRU "C".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GOBACK            PIC 9 VALUE 1.
       01  NULL              PIC X(2) VALUE "NU".
       01  TYPEDEF           PIC X(2) VALUE "TD".
       01  WS-T.
           05  SELF          PIC X OCCURS 3 INDEXED BY OBJECT.
       01  X                 PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "P" TO SELF (1)
           MOVE "Q" TO SELF (GOBACK + 1)
           SET OBJECT TO 3
           MOVE "R" TO SELF (OBJECT)
           DISPLAY "G=" GOBACK " " NULL " " TYPEDEF
           DISPLAY "S=" SELF (1) SELF (2) SELF (3)
           IF X IS RETURNING DISPLAY "C=Y" ELSE DISPLAY "C=N" END-IF
           STOP RUN.
