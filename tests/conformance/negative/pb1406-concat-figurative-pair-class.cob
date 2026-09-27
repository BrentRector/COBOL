      *> reject-at: 2002 2014 2023
      *> kb/Work PB1406 - ISO 8.8.3.1 is {literal-1 | concatenation-
      *> expression-1} & literal-2, so SPACE & SPACE & N"AB" is
      *> (SPACE & SPACE) & N"AB". By 8.8.3.3 GR1b the inner expression
      *> is class alphanumeric, and it is a concatenation expression,
      *> not a figurative constant - so the outer pair is alphanumeric &
      *> national, which 8.8.3.2 SR1 forbids: COBOLNET1540. (Classing the
      *> chain by its first non-figurative operand accepted it.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1406FPC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC N(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE SPACE & SPACE & N"AB" TO W-N.
           DISPLAY W-N.
           STOP RUN.
