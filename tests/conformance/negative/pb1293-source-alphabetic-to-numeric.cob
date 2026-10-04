      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1293 - ISO 13.18.53.3 SR2: "If identifier-1 is specified without the ROUNDED phrase, identifier-1 shall be described such that a MOVE statement
      *> is valid with identifier-1 as the sending operand and the printable item as the receiving operand."   cite.py: OK  13.18.53.3 2)  (Syntax rules)
      *> 14.9.25.3 SR10, Table 16: an alphabetic operand does not move to a numeric item.  Here: SOURCE WA (PIC A(3)) into PIC 9(2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB1293SRCALPHATONUM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "W13PPB1293SRCALPHATONUM.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WA PIC A(3) VALUE "ABC".
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 9(2) SOURCE WA.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
