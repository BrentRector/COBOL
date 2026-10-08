      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1943 - ISO 8.4.3.3.3 SR3: "Identifier-1 shall not be a reference-modification format identifier."
      *>   cite.py: OK  8.4.3.3.3 3)  (Syntax rules)
      *> A reference-modified sum counter is a reference-modified identifier like any other, so reference-modifying it
      *> again - CF-T (1:3) (2:1) - is the SR3 violation. (The counter's single modification is legal, 8.4.3.3.3 SR1:
      *> 85/pb1943_sum_counter_ref_mod.) Before the fix the counter's name was not found at all and the statement drew
      *> COBOLNET1639 "'CF-T(1:3)(2:1)' is not defined", the wrong rule.
      *>   cite.py: OK  13.18.54.4 5)  (General rules)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1943N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1943N1.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-X     PIC 9999 VALUE 1234.
       01  WS-A     PIC X    VALUE SPACE.
       REPORT SECTION.
       RD  R1 CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "DET".
       01  CFG TYPE IS CONTROL FOOTING FINAL.
           02  LINE PLUS 1.
               03  CF-T COLUMN 1  PIC 9999 SUM WS-X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DET.
           MOVE CF-T (1:3) (2:1) TO WS-A.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
