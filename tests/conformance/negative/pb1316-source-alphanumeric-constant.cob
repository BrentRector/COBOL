      *> reject-at: 2002 2014 2023
      *> kb/Work PB1316 - ISO 13.10.3 SR2: constant-name-1 may be used "anywhere that a format specifies a literal of the
      *> class and category of constant-name-1". The SOURCE operand is `{ identifier-1 | arithmetic-expression-1 }`
      *> (13.18.53.2); a constant-name is not an identifier, and an arithmetic-expression-1 operand may be a NUMERIC
      *> literal only (8.8.1.1), so the ALPHANUMERIC constant KS is no SOURCE operand - and 13.18.53.3 SR3 asks the
      *> entry of an arithmetic-expression-1 for a numeric item, which PIC X(2) is not. COBOLNET2141.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1316N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1316n1.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  KS CONSTANT AS "AB".
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 30 LINES.
       01  DET-A TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) SOURCE KS.
       PROCEDURE DIVISION.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE DET-A.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
