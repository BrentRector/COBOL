      *> reject-at: 85
      *> kb/Work PB1627 - ISO 8.3.3.6.3 SR2 lets the literal-1 of ALL
      *> literal-1 be a concatenation expression, whose operands include a
      *> figurative constant (8.8.3.2 SR1). The concatenation operator is a
      *> COBOL-2002 introduction (8.8.3), so below 2002 the & in the
      *> literal-1 is gated: COBOLNET0900 (concat-operator-2002).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1627B02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(6).
       PROCEDURE DIVISION.
           MOVE ALL "A" & SPACE TO W-X.
           STOP RUN.
