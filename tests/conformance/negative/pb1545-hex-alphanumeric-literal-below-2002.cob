      *> reject-at: 85
      *> kb/Work PB1545 - the hexadecimal alphanumeric literal X"..." (ISO §8.3.3.2.2 format 2) is a COBOL-2002
      *> literal format: ANSI X3.23-1985 has no hexadecimal literal. VCR row 7.29 states the derived edge. The
      *> literal is written only in a VALUE clause of an alphanumeric item, the one position no data-category
      *> gate covers, so the refusal can come only from the token gate (LiteralScreenPass.GateFormat).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1545N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC XX VALUE X"4142".
       PROCEDURE DIVISION.
           DISPLAY V
           STOP RUN.
