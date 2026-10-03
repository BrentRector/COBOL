      *> kb/Work PB1403 - ISO 8.3.2.2: "With the exception of section-names, paragraph-names, and level-numbers, each
      *> user-defined word shall contain at least one basic letter or extended letter" (cite.py --check 8.3.2.2 "each
      *> user-defined word shall contain at least one basic letter or extended letter"). This is the positive side:
      *> the three exempt types may be LETTERLESS, and a word with a letter anywhere in it is no exception at all.
      *>   1-2   - a SECTION-name with no letter; 3-4 - a PARAGRAPH-name with no letter (the level-number 01 is digits
      *>           too, and 8.3.2.2 exception 2 even lets it match a paragraph-name or section-name).
      *>   A1-2 / 42-DATA - data-names whose digits are joined to a letter: legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1403POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A1-2 PIC X VALUE "A".
       01  42-DATA PIC X VALUE "B".
       PROCEDURE DIVISION.
       1-2 SECTION.
       3-4.
           DISPLAY A1-2 42-DATA
           STOP RUN.
