      *> reject-at: 2002 2014 2023
      *> kb/Work PB1393 - ISO 8.3.3.2.3 SR6: "Each hex-character-sequence-1
      *> shall consist of the number of hexadecimal digits that the
      *> implementor has specified as the number of hexadecimal digits that
      *> map to an alphanumeric character" - two here. The rule is the
      *> LITERAL's, wherever it is written; as a concatenation-expression
      *> operand (8.8.3, a concatOperand rather than a nonNumericLiteral)
      *> X"4" used to escape the check and decode to the empty string, so
      *> W was "Z" and LENGTH("Z" & X"4") answered 1. COBOLNET1635 at every
      *> edition that has the & operator (2002+).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB1393HX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "Z" & X"4" TO W.
           DISPLAY W.
           STOP RUN.
