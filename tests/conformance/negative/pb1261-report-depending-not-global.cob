      *> reject-at: 2002 2014 2023
      *> kb/Work PB1261 - ISO 13.18.38.3 SR18 is a FORMATS 2 AND 3 rule: a report group entry is
      *> subordinate to its RD, and RD RPT carries the GLOBAL clause (13.18.27.3 SR1 e)), so the
      *> DEPENDING object N shall be a global name; it is local. (The report OCCURS clause arrived
      *> in COBOL 2002.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1261NRD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "PB1261NRD.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 2.
       REPORT SECTION.
       RD RPT GLOBAL.
       01 TYPE DETAIL.
           02 LINE PLUS 1.
               03 COLUMN 1 PIC X VALUE "A" OCCURS 1 TO 3 DEPENDING ON N
                  STEP 2.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
