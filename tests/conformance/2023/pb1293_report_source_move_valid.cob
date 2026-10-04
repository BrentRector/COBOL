      *> kb/Work PB1293 - ISO 13.18.53.3 SR2 and 13.18.53.4 GR1: a SOURCE clause identifier-1 (without ROUNDED) is the sending operand of an implicit MOVE to the
      *> printable item, so every pairing 14.9.25.3 Table 16 marks valid compiles and moves by the MOVE rules.  The negative twins
      *> (pb1293-source-numeric-to-alphabetic, pb1293-source-alphabetic-to-numeric) are the pairings the table forbids.
      *>   col  1  PIC X(3)  SOURCE WS-N (PIC 9(3) = 42)   numeric integer -> alphanumeric: valid; 14.6.8 alignment of the digits "042".
      *>   col  5  PIC 9(2)  SOURCE WS-T (PIC X(2) = "17") alphanumeric -> numeric: valid; the characters "17" are the value 17.
      *>   col  8  PIC ZZ9   SOURCE WS-N                   numeric -> numeric-edited: valid; 42 edited " 42".
      *> The program reads the report file back and shows each window.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB1293SRCMOVEOK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1293src.txt".
           SELECT CHK ASSIGN TO "pb1293src.txt"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X(40).
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       01  WS-N   PIC 9(3) VALUE 42.
       01  WS-T   PIC X(2) VALUE "17".
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(3) SOURCE WS-N.
           03  COLUMN 5 PIC 9(2) SOURCE WS-T.
           03  COLUMN 8 PIC ZZ9 SOURCE WS-N.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM SHOW-LINE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           STOP RUN.
       SHOW-LINE.
           IF CHK-REC NOT = SPACES
               DISPLAY "X=[" CHK-REC(1:3) "] 9=[" CHK-REC(5:2)
                   "] Z=[" CHK-REC(8:3) "]"
           END-IF.
