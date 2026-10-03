      *> THE COLUMN CLAUSE'S LEFT / CENTER / RIGHT ALIGNMENT PHRASE (kb/Work PB1220). It had no grammar surface:
      *> `COLUMN CENTER 5` was COBOL0309 and `COLUMN RIGHT 10` COBOL0001, so legal source was refused and
      *> GR6 c)/d) and GR9 for an aligned item could not exist. The phrase is a COBOL-2002 form (construct
      *> report-multi-column-2002; the COBOL-85 form was COLUMN NUMBER IS integer-1 alone).
      *>
      *> THE RULES (ISO/IEC 1989:2023 13.18.14, every quotation run through cite.py --check):
      *> 13.18.14.4 GR6 b) LEFT: "integer-1 is the leftmost column of the printable item".
      *> 13.18.14.4 GR6 c) RIGHT: "integer-1 is the rightmost column ... The leftmost column of the printable
      *>   item is integer-1 - printable-size + 1".
      *> 13.18.14.4 GR6 d) 1. CENTER, odd size: "The leftmost column is integer-1 - ((printable-size - 1) / 2);
      *>   the rightmost column is integer-1 + (printable-size / 2), truncated to an integer".
      *> 13.18.14.4 GR6 d) 2. CENTER, even size: "The leftmost column is (integer-1 - (printable-size / 2)) + 1;
      *>   the rightmost column is integer-1 + (printable-size / 2)".
      *> 13.18.14.3 SR9: "If any of the operands is absolute and neither LEFT, CENTER, nor RIGHT is specified,
      *>   LEFT is assumed". 13.18.14.4 GR9: "The rightmost column position of each printable item becomes the
      *>   new value of the horizontal counter", and GR8 puts a relative item at counter + integer-2 - so a
      *>   relative item AFTER an aligned one is placed from the aligned item's RIGHTMOST column.
      *> The alignment phrase stands before IS/ARE and serves every operand of a multiple COLUMN clause.
      *>
      *> DERIVATION (no PAGE clause: 13.18.39.4 GR2 a), so only the printed lines are asserted; all-space lines
      *> are skipped in the readback):
      *>   line 1  RIGHT 10 x3 -> 8-10 RRR | CENTER 15 x3 (odd) -> 14-16 OOO | CENTER 20 x4 (even) -> 19-22
      *>           EEEE | LEFT IS 30 -> 30 L | bare 33 (LEFT assumed) -> 33 D
      *>   line 2  RIGHT 10 -> 8-10 RRR, counter 10; PLUS 2 -> 12 P; CENTER 20 x4 -> 19-22 EEEE, counter 22;
      *>           PLUS 1 -> 23 Q; CENTER 30 x3 -> 29-31 OOO, counter 31; PLUS 3 -> 34 S
      *>   line 3  COLUMNS CENTER ARE 5 15 25, size 3, one VALUE cycling over the three operands (13.18.63.4
      *>           GR23): 4-6, 14-16, 24-26 MMM
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1220AL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1220-align.rpt".
           SELECT CHK ASSIGN TO "pb1220-align.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       FD CHK.
       01 CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01 WS-EOF  PIC X     VALUE "N".
       01 WS-I    PIC 99    VALUE 0.
       01 WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD R-1.
       01 DET-A TYPE DE.
          02 LINE PLUS 1.
             03 COLUMN RIGHT 10 PIC XXX VALUE "RRR".
             03 COLUMN CENTER 15 PIC XXX VALUE "OOO".
             03 COLUMN CENTER 20 PIC XXXX VALUE "EEEE".
             03 COLUMN LEFT IS 30 PIC X VALUE "L".
             03 COLUMN 33 PIC X VALUE "D".
          02 LINE PLUS 1.
             03 COLUMN RIGHT 10 PIC XXX VALUE "RRR".
             03 COLUMN PLUS 2 PIC X VALUE "P".
             03 COLUMN CENTER 20 PIC XXXX VALUE "EEEE".
             03 COLUMN PLUS 1 PIC X VALUE "Q".
             03 COLUMN CENTER 30 PIC XXX VALUE "OOO".
             03 COLUMN PLUS 3 PIC X VALUE "S".
          02 LINE PLUS 1.
             03 COLUMNS CENTER ARE 5 15 25 PIC XXX VALUE "MMM".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-A.
           TERMINATE R-1.
           CLOSE RPT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-LINE NOT = SPACES
               DISPLAY "[" WS-LINE "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
