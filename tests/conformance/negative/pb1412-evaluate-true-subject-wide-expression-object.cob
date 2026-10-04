      *> reject-at: 2002 2014 2023
      *> kb/Work PB1412. The OBJECT twin of pb1412-evaluate-wide-expression-subject-true-object. ISO 1989:2023 14.9.13.3
      *> SR6 a) makes a boolean-expression selection object condition-2 only when it "results in one boolean character";
      *> A B-AND C over two PIC 1(4) items results in four (8.8.2 rule 10), so it stays boolean-expression-2 and Table
      *> 15 (SR10) leaves the TRUE-or-FALSE subject x Boolean-expression object cell BLANK: COBOLNET1634.
      *>   cite.py --check 14.9.13.3 "If the selection subject is TRUE or FALSE and the selection object is a boolean
      *>     expression that results in one boolean character" -> OK 6) a)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1412NET.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 1(4) VALUE B"1100".
       01 C PIC 1(4) VALUE B"1010".
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE TRUE
              WHEN A B-AND C DISPLAY "Y"
              WHEN OTHER DISPLAY "N"
           END-EVALUATE
           STOP RUN.
