      *> reject-at: 2002 2014 2023
      *> kb/Work PB1412. ISO 1989:2023 14.9.13.3 SR6 b) reclassifies a boolean-expression selection subject to a boolean
      *> CONDITION only when it "results in one boolean character". A B-AND C over two PIC 1(4) items results in FOUR
      *> (8.8.2 rule 10), so it stays boolean-expression-1, and SR10's Table 15 leaves the Boolean-expression subject x
      *> TRUE-or-FALSE object cell BLANK: a syntax-rule violation, so a compile-time COBOLNET1634 - and no second
      *> diagnostic about the word TRUE read as a condition. (The one-character spelling is legal and is pinned by
      *> tests/conformance/2002/pb1412_evaluate_boolean_expression_operands.)
      *>   cite.py --check 14.9.13.3 "If the selection object is TRUE or FALSE and the selection subject is a boolean
      *>     expression that results in one boolean character" -> OK 6) b)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1412NEW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 1(4) VALUE B"1100".
       01 C PIC 1(4) VALUE B"1010".
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE A B-AND C
              WHEN TRUE DISPLAY "T"
              WHEN OTHER DISPLAY "O"
           END-EVALUATE
           STOP RUN.
