      *>--- conformance-case
      *> id: pb655_context_sensitive_user_words
      *> rules: [SR-8.3.2.1-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 8.3.2.1 3) - "Context-sensitive words may be used as user-defined words
      *> and system-names in contexts other than the language construct in which they
      *> are defined" (kb/Work PB655). SIGNED, RECURSIVE, CYCLE, PREVIOUS and YYYYMMDD
      *> are 8.10 context-sensitive words with no 8.9 row at any edition, so every use
      *> below is legal at 85, 2002, 2014 and 2023 - and each was a raw COBOL0001 parse
      *> error because the lexer tokenizes the word and no cobolWord row admitted it.
      *> Legs: plain names, a subscripted table, qualification, non-first DISPLAY
      *> operands. Expected output read off the VALUE clauses and the MOVEs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB655-CS-WORDS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-G.
           05  SIGNED        PIC X(2) VALUE "SG".
           05  RECURSIVE     PIC X(2) VALUE "RC".
           05  CYCLE         PIC X(2) VALUE "CY".
       01  WS-T.
           05  PREVIOUS      PIC X(2) OCCURS 2.
       01  YYYYMMDD          PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "P1" TO PREVIOUS (1)
           MOVE "P2" TO PREVIOUS (YYYYMMDD)
           DISPLAY "S=" SIGNED " " RECURSIVE " " CYCLE
           DISPLAY "Q=" CYCLE OF WS-G
           DISPLAY "P=" PREVIOUS (1) PREVIOUS (2) " Y=" YYYYMMDD
           STOP RUN.
