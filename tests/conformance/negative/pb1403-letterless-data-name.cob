      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1403 - ISO 8.3.2.2: "With the exception of section-names, paragraph-names, and level-numbers, each
      *> user-defined word shall contain at least one basic letter or extended letter" (cite.py --check 8.3.2.2 "each
      *> user-defined word shall contain at least one basic letter or extended letter"). 1-2 is a legal PARAGRAPH-name
      *> (the lexer must admit the token, which is why nothing screened it), but as a DATA-name it has no letter. It
      *> compiled and ran.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB1403.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  1-2 PIC X VALUE "A".
       PROCEDURE DIVISION.
           DISPLAY 1-2
           STOP RUN.
