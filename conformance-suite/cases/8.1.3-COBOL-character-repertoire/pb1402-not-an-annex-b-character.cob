*>--- conformance-case
*> id: pb1402-not-an-annex-b-character
*> rules: [GR-8.1.3.2-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.3.2.1 / Annex B (kb/Work PB1402): U+20AC EURO SIGN is no character of Annex B, so it cannot
      *> stand in a COBOL word; the lexer keeps it in the word and the Annex B screen names it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1402N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PREIS€ PIC 9(3) VALUE 5.
       PROCEDURE DIVISION.
           DISPLAY PREIS€.
           STOP RUN.
