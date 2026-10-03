      *> reject-at: 2002 2014 2023
      *> kb/Work PB1306 - ISO 13.18.64.3 SR2: "This definition of data-name-1 may be referenced only within the current
      *> entry or a subordinate entry." The second LINE entry is neither the entry that defines K nor subordinate to
      *> it, so its SOURCE K names nothing: COBOLNET1639 (the name is not defined there).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1306N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1306n3.txt".
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
                   VARYING K SOURCE K.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC 9 SOURCE K.
       PROCEDURE DIVISION.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE DET-A.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
