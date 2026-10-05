      *> kb/Work PB1373 (and PB2003) - ISO/IEC 1989:2023 7.3.10 COBOL-WORDS, read as the standard writes it.
      *> (1) 8.3.5 2): "The COBOL characters comma and semicolon, immediately followed by a space, are separators that
      *>     may be used anywhere the separator space is used."  EQUATE "DISPLAY", WITH "SHOW" and EQUATE
      *>     "IDENTIFICATION"; WITH "IDENT" are the entries a space writes; SHOW is DISPLAY (7.3.10.4 GR2).
      *> (2) 7.3.10.3 SR1: "The COBOL-WORDS directive may be specified only before the first IDENTIFICATION DIVISION" -
      *>     and by 7.3.10.4 GR2 IDENT DIVISION. IS that division's header, so the entries above it are legal and the
      *>     program is read with the group's synonyms.
      *> (3) 7.3.10.3 SR4: the fresh word is "a COBOL word that meets the requirements for a user-defined data-name" and is
      *>     none of the 2023 reserved, context-sensitive or intrinsic-function words: AUTHOR (a 1985 reserved word that
      *>     8.9 no longer lists) and MY_WORD (8.3.2.1: the underscore is a word character) are both fresh words.
      *> (4) 7.2.1: directives are "syntactically correct in the initial source text and library text" - the omitted
      *>     branch below holds a TURN and a COBOL-WORDS directive that are, separators and all, so nothing is reported.
      *> Each leg can fail: a space-only tokenizer reads the commas into the words (1623); the unit boundary read on the
      *> raw text misses IDENT and judges nothing (the negative half is negative/pb1373-cobol-words-sr1-equated-header);
      *> a lexer-keyword population refuses RESERVE "AUTHOR"; a word-shape check without the underscore refuses MY_WORD.
       >>COBOL-WORDS EQUATE "DISPLAY", WITH "SHOW"
       >>COBOL-WORDS EQUATE "IDENTIFICATION"; WITH "IDENT"
       >>COBOL-WORDS RESERVE "AUTHOR"
       >>COBOL-WORDS RESERVE "MY_WORD"
       >>IF 1 = 2
       >>TURN EC-SIZE, EC-BOUND CHECKING ON
       >>COBOL-WORDS EQUATE "MOVE"; WITH "PUT"
       >>END-IF
       IDENT DIVISION.
       PROGRAM-ID. PB1373SEPARATORS.
       PROCEDURE DIVISION.
           SHOW "PB1373 OK"
           STOP RUN.
