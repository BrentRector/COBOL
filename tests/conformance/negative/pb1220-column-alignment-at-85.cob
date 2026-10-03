      *> reject-at: 85
      *> kb/Work PB1220. The COLUMN clause's LEFT / CENTER / RIGHT alignment phrase (ISO/IEC 1989:2023
      *> 13.18.14.2 Format 1, 13.18.14.4 GR6) is a COBOL-2002 form (construct report-multi-column-2002): the
      *> COBOL-85 COLUMN clause is COLUMN NUMBER IS integer-1 alone, so COLUMN CENTER 5 is COBOLNET0900 at
      *> --std 85. The positive twin is conformance:2002/pb1220_column_alignment.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1220N8.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1220-85.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       REPORT SECTION.
       RD R-1.
       01 DET-A TYPE DE.
          02 LINE PLUS 1.
             03 COLUMN CENTER 5 PIC XXX VALUE "CCC".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
