      *> reject-at: 2002 2014 2023
      *> kb/Work PB1306 - ISO 13.18.64.3 SR2: "Data-name-1 shall not be defined elsewhere in the source element, except as
      *> data-name-1 in another VARYING clause of an entry not subordinate to the subject of the current entry."
      *> A second K in the SAME VARYING clause is neither: it is a second definition inside the one entry.
      *> COBOLNET1559.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1306N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1306n2.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-X PIC 9 VALUE 0.
       REPORT SECTION.
       RD  R1.
       01  DET-A TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC 9 OCCURS 3 TIMES STEP 3
                   VARYING K FROM 1 K FROM 2 SOURCE K.
       PROCEDURE DIVISION.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE DET-A.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
