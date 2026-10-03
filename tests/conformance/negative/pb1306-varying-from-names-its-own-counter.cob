      *> reject-at: 2002 2014 2023
      *> kb/Work PB1306 - ISO 13.18.64.3 SR3: "Data-name-1 shall not be referenced in arithmetic-expression-1 of the same
      *> VARYING clause, but may be referenced in arithmetic-expression-2 of the same VARYING clause or in
      *> arithmetic-expression-1 or arithmetic-expression-2 of a VARYING clause in a subordinate entry."
      *> FROM K defines K from itself. COBOLNET1559. (The BY form, `BY K`, is the legal half - see
      *> conformance:2002/pb1306_varying_scope.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1306N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1306n4.txt".
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
                   VARYING K FROM K SOURCE K.
       PROCEDURE DIVISION.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE DET-A.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
