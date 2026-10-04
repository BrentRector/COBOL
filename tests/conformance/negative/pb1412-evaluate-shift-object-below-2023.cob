      *> reject-at: 2002 2014
      *> kb/Work PB1412. The OBJECT twin of pb1412-evaluate-shift-subject-below-2023: `WHEN A B-SHIFT-L 1` is
      *> [ NOT ] boolean-expression-2 of ISO 1989:2023 14.9.13.2, and the boolean shift operators are a COBOL-2023
      *> introduction (8.8.2 rule 8, Annex E.2), so it is refused below 2023 (COBOLNET0900) at the second EVALUATE site.
      *>   cite.py --check 8.8.2 "Boolean shift operations shall be performed without regard for the usage of
      *>     the first operand." -> OK 8)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1412NEO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 1(4) VALUE B"1100".
       01 E PIC 1(4) VALUE B"1000".
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE E
              WHEN A B-SHIFT-L 1 DISPLAY "Y"
              WHEN OTHER DISPLAY "N"
           END-EVALUATE
           STOP RUN.
