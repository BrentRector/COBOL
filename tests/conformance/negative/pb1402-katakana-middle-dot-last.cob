      *> reject-at: 2023
      *> Annex B.3 item 3 (kb/Work PB1402): from COBOL 2023 U+30FB KATAKANA MIDDLE DOT is permitted only inside
      *> a word, neither first nor last (Annex E.2 item 4) - before 2023 it could end one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1402N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ABC・ PIC X VALUE "M".
       PROCEDURE DIVISION.
           DISPLAY ABC・.
           STOP RUN.
