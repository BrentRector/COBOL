      *> kb/Work PB1034. ISO 1989:2023 8.8.4.2.2 Format 1 (General-relation), read off the RENDERED page (PDF p217,
      *> printed 187): IS [NOT] GREATER THAN, IS [NOT] >, IS [NOT] LESS THAN, IS [NOT] <, IS [NOT] EQUAL TO,
      *> IS [NOT] =, IS <>, IS GREATER THAN OR EQUAL TO, IS >=, IS LESS THAN OR EQUAL TO, IS <=. IS is not underlined
      *> and THAN / TO are not underlined, so each is optional (5.2.3). This is the positive half: every printed
      *> spelling EXCEPT the symbol <> compiles at every edition and evaluates by the rule 8.8.4.2.4 gives numeric
      *> operands. `<>` is a COBOL-2002 introduction (VCR row 7.26, kb/Work PB1459: COBOLNET0900 below 2002), so
      *> its printed `IS <>` spelling is asserted in 2002/pb1034_is_not_equal_symbol. The five
      *> spellings it does NOT print (NOT >=, NOT <=, NOT GREATER THAN OR EQUAL TO, NOT LESS THAN OR EQUAL TO, and
      *> EQUAL THAN) are the pb1034-* negatives.
      *>   cite.py --check 8.8.4.2.4 "For operands whose class is numeric, a comparison is made with respect to the
      *>     algebraic value of the operands" -> OK
      *> A = 5, B = 3, C = 5. A against B: greater, so GREATER THAN / > / NOT LESS THAN / NOT < / NOT = /
      *> GREATER THAN OR EQUAL TO / >= are true; their complements false. A against C: equal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1034POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 5.
       01 B PIC 9 VALUE 3.
       01 C PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           IF A IS GREATER THAN B DISPLAY "01T" ELSE DISPLAY "01F".
           IF A IS NOT GREATER THAN B DISPLAY "02T" ELSE DISPLAY "02F".
           IF A > B DISPLAY "03T" ELSE DISPLAY "03F".
           IF A NOT > B DISPLAY "04T" ELSE DISPLAY "04F".
           IF A IS LESS THAN B DISPLAY "05T" ELSE DISPLAY "05F".
           IF A IS NOT LESS THAN B DISPLAY "06T" ELSE DISPLAY "06F".
           IF A < B DISPLAY "07T" ELSE DISPLAY "07F".
           IF A NOT < B DISPLAY "08T" ELSE DISPLAY "08F".
           IF A IS EQUAL TO B DISPLAY "09T" ELSE DISPLAY "09F".
           IF A IS NOT EQUAL TO B DISPLAY "10T" ELSE DISPLAY "10F".
           IF A = B DISPLAY "11T" ELSE DISPLAY "11F".
           IF A NOT = B DISPLAY "12T" ELSE DISPLAY "12F".
           IF A IS GREATER THAN OR EQUAL TO B DISPLAY "13T"
              ELSE DISPLAY "13F".
           IF A IS >= B DISPLAY "14T" ELSE DISPLAY "14F".
           IF A IS LESS THAN OR EQUAL TO B DISPLAY "15T"
              ELSE DISPLAY "15F".
           IF A IS <= B DISPLAY "16T" ELSE DISPLAY "16F".
           IF A GREATER OR EQUAL C DISPLAY "17T" ELSE DISPLAY "17F".
           IF A LESS OR EQUAL C DISPLAY "18T" ELSE DISPLAY "18F".
           IF A >= C DISPLAY "19T" ELSE DISPLAY "19F".
           IF A <= C DISPLAY "20T" ELSE DISPLAY "20F".
           IF A GREATER C DISPLAY "21T" ELSE DISPLAY "21F".
           IF A IS EQUAL C DISPLAY "22T" ELSE DISPLAY "22F".
           STOP RUN.
