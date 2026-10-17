      *>--- conformance-case
      *> id: declined_validate_words_are_user_words
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> THE 85 COMPLEMENT of the A.4.14 / A.4.3 declines: every word the new declined-facility grammar
      *> tokenizes is a legal USER-DEFINED WORD at COBOL-85, and adding a lexer token for it must not change
      *> that. DESTINATION is 8.9-reserved at every edition and so is NOT here; VALIDATE-STATUS, VAL-STATUS,
      *> VALID and FORMAT are "added 2002"; APPLY, NONE and RELATION are 8.10 context-sensitive and never
      *> 8.9-reserved. 13.18.62.3 SR9 makes VAL-STATUS and VALIDATE-STATUS equivalent WORDS, an edition-level
      *> reservation that survives the module decline - "DOCUMENTED-NON-SUPPORT on the equivalence rule" is
      *> not a licence to drop the words, and this fixture is what would catch that regression.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLWORDS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 VAL-STATUS      PIC X(4) VALUE "V1".
       01 VALIDATE-STATUS PIC X(4) VALUE "V2".
       01 APPLY           PIC X(4) VALUE "V3".
       01 NONE            PIC X(4) VALUE "V4".
       01 RELATION        PIC X(4) VALUE "V5".
       01 VALID           PIC X(4) VALUE "V6".
       01 FORMAT          PIC X(4) VALUE "V7".
       01 DEFAULT-X       PIC 9(2) VALUE 8.
          88 DEFAULT-OK   VALUE 8 THRU 9.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY VAL-STATUS " " VALIDATE-STATUS " " APPLY.
           DISPLAY NONE " " RELATION " " VALID " " FORMAT.
           IF DEFAULT-OK DISPLAY "COND-88-OK" END-IF.
           STOP RUN.
