      *> kb/Work PB1034 (the `<>` half of 85/pb1034_printed_relational_operator_set), moved to its introducing
      *> edition: `<>` is a COBOL-2002 relational operator (VCR row 7.26, kb/Work PB1459; COBOLNET0900 below
      *> 2002, so COBOL-85 cannot carry it). ISO 1989:2023 8.8.4.2.2 Format 1 prints the alternative `IS <>`
      *> with IS (not underlined, optional) and WITHOUT the NOT bracket that the first six alternatives carry,
      *> so `IS <>` is the whole printed spelling.
      *>   cite.py --check 8.8.4.2.2 "IS" -> OK (General format, Format 1)
      *>   cite.py --check 8.8.4.2.4 "For operands whose class is numeric, a comparison is made with respect to
      *>     the algebraic value of the operands" -> OK
      *> A = 5, B = 3, C = 5. 5 and 3 differ, so A IS <> B is true; 5 and 5 are equal, so A IS <> C is false;
      *> the omitted IS spelling `A <> B` agrees with the printed one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1034ISNE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 5.
       01 B PIC 9 VALUE 3.
       01 C PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           IF A IS <> B DISPLAY "01T" ELSE DISPLAY "01F".
           IF A IS <> C DISPLAY "02T" ELSE DISPLAY "02F".
           IF A <> B DISPLAY "03T" ELSE DISPLAY "03F".
           STOP RUN.
