      *> reject-at: 2002 2014 2023
      *> A ZERO CONSTANT-NAME AS THE OCCURS integer-2 (kb/Work PB1039).
      *> ISO/IEC 1989:2023 §5.5 1): an integer-n "shall be unsigned and nonzero unless
      *> otherwise specified in the associated rules", and §13.10.4 GR1 makes OCCURS ZC
      *> TIMES "as if" OCCURS 0 TIMES were written. No rule of the fixed-occurrence format
      *> permits zero, so the constant is refused exactly as the literal is: COBOLNET2386.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1039OZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ZC CONSTANT AS 0.
       01 T.
          05 E PIC X OCCURS ZC TIMES.
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
