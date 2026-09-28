      *> reject-at: 85
      *> kb/Work PB1305 - the report VARYING clause (§13.18.64) is a
      *> COBOL-2002 introduction; COBOL-85 has no VARYING clause in a
      *> report group description entry, so the clause is refused at
      *> 85 by its introduction gate (COBOLNET0900). The positive twins
      *> are conformance:2002/pb1305_report_varying_expression and
      *> conformance:2014/pb1305_report_varying_standard_decimal.
      *> "Each entry containing a VARYING clause establishes an
      *> independent temporary integer data item that shall be large
      *> enough to contain the maximum expected value."
      *>   cite.py: OK  §13.18.64.4 1)  (General rules)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1305R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1305r.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-V.
       REPORT SECTION.
       RD  R-V.
       01  DET-A TYPE DE.
           02  LINE PLUS 1.
               03  COLUMNS 1 3 5 PIC 9 VARYING K FROM 2 * 3 - 5
                   BY 4 - 2 SOURCE K.
       PROCEDURE DIVISION.
       M1.
           OPEN OUTPUT PRT.
           INITIATE R-V.
           GENERATE DET-A.
           TERMINATE R-V.
           CLOSE PRT.
           STOP RUN.
