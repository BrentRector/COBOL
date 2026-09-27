      *> reject-at: 2002 2014 2023
      *> kb/Work PB1406 - ISO 8.8.3.1: the operands of a concatenation
      *> expression are literal-1 and literal-2. The only words that
      *> stand for a literal are a constant-name (13.10.3 SR2) and a
      *> symbolic-character (12.3.7.4 GR11); W-Y is a data-name, so it
      *> is not an operand: COBOLNET2473.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1406NLT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-Y PIC X(4) VALUE "ABCD".
       01 W-X PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "X" & W-Y TO W-X.
           DISPLAY W-X.
           STOP RUN.
