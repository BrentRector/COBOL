      *> reject-at: 2002 2014 2023
      *> kb/Work PB1306 - ISO 13.18.64.3 SR2: "Data-name-1 shall not be defined elsewhere in the source element, except as
      *> data-name-1 in another VARYING clause of an entry not subordinate to the subject of the current entry."
      *> The 04 entry is SUBORDINATE to the 03 entry whose VARYING clause already defines K, so the second VARYING K
      *> is not the permitted reuse (an entry NOT subordinate, whose counter would be an independent data item).
      *> COBOLNET1559 (the report group clause rule family, 13.15.3 / 13.18.64.3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1306N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1306n1.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-X PIC 9 VALUE 0.
       REPORT SECTION.
       RD  R1.
       01  DET-A TYPE DE.
           02  LINE PLUS 1.
               03  OCCURS 2 TIMES STEP 10 VARYING K FROM 1 BY 1.
                   04  COLUMN 1 PIC 9 OCCURS 2 TIMES STEP 3
                       VARYING K FROM 3 SOURCE K.
       PROCEDURE DIVISION.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE DET-A.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
