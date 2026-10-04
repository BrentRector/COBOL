      *> kb/Work PB1294 - ISO 13.18.54.4 GR8: a rolled total whose data-name-1 is a REPEATING item.
      *> GR8: "If the addend is data-name-1 and data-name-1 is a repeating item, repetitions of the addend are either
      *>   all added into the same sum counter or are each added into a different corresponding occurrence of the
      *>   sum counter, according to the following rules: a) If the addend and the sum counter are subject to the same
      *>   number of levels of repetition, each occurrence of the addend is added into the corresponding occurrence of
      *>   the sum counter. b) If the addend is subject to a greater number of levels of repetition than the sum
      *>   counter, ... forming the total of a complete table of occurrences of the addend ... into each occurrence of
      *>   the sum counter."
      *>   cite.py: OK  13.18.54.4 8)  (General rules)
      *> SR4 b): data-name-1 in the SAME report group description as the subject "shall be a repeating item ... and
      *>   subject to at least one more level of repetition than the subject of the entry".
      *>   cite.py: OK  13.18.54.3 4) b)  (Syntax rules)
      *> SR4 c): in a DIFFERENT report group description it "either shall not reference a repeating item or shall
      *>   reference a repeating item that is subject to at least the same number of levels of repetition as the
      *>   subject of the entry".   cite.py: OK  13.18.54.3 4) c)  (Syntax rules)
      *> SR4 d): "The maximum number of repetitions of data-name-1 and the subject of the entry shall be equal at each
      *>   corresponding level".   cite.py: OK  13.18.54.3 4)  (Syntax rules)
      *> GR7 b): "If data-name-1 is the name of an entry in the current report group description, adding takes place
      *>   during the processing of the current report group before any of the report group's lines are printed."
      *>   cite.py: OK  13.18.54.4 7) b)  (General rules)
      *> The SOURCE operand of each occurrence of CELL is the operand its repetition takes (13.18.53.4 GR4: "successive
      *>   operands are assigned to successive repeating printable items").
      *>   cite.py: OK  13.18.53.4 4)  (General rules)
      *>
      *> DERIVATION. DET has CELL, a repeating item (OCCURS 3 TIMES STEP 3) over operands WS-A WS-B WS-C, and ROWT, a
      *> SUM in the SAME group over CELL: CELL has one level of repetition and ROWT none (SR4 b: one more), so every
      *> occurrence of CELL is added into the ONE counter ROWT (GR8 b), during the processing of DET (GR7 b).
      *> CFF, a FINAL control footing (SR4 f: it may name a detail's entry), has COLT, OCCURS 3 TIMES, SUM CELL: both
      *> have one level of repetition of maximum 3 (SR4 c and d), so occurrence k of CELL is added into occurrence k of
      *> COLT (GR8 a) when DET is processed. GRAND sums ROWT, a non-repeating entry of a different group (SR4 c).
      *> Rows (WS-A, WS-B, WS-C): (1,2,3) and (10,20,30). ROWT: 6 and 60; COLT: 11 22 33; GRAND: 66.
      *> CELL is PIC 99 at columns 1, 4, 7 (STEP 3); ROWT is PIC 999 at column 12; COLT is PIC 999 at columns 1, 5, 9
      *> (STEP 4) and GRAND PIC 9999 at column 13. Line 1: `01 02 03   006`, line 2: `10 20 30   060`, line 3 (the
      *> footing at TERMINATE): `011 022 033 0066`.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1294REP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1294REP.TXT".
           SELECT CHK ASSIGN TO "PB1294REP.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-A    PIC 99    VALUE 0.
       01  WS-B    PIC 99    VALUE 0.
       01  WS-C    PIC 99    VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R CONTROL IS FINAL.
       01  DET TYPE DE LINE PLUS 1.
           05  CELL COLUMN 1 PIC 99 OCCURS 3 TIMES STEP 3
               SOURCE WS-A WS-B WS-C.
           05  ROWT COLUMN 12 PIC 999 SUM CELL.
       01  CFF TYPE CF FINAL LINE PLUS 1.
           05  COLT COLUMN 1 PIC 999 OCCURS 3 TIMES STEP 4 SUM CELL.
           05  GRAND COLUMN 13 PIC 9999 SUM ROWT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           MOVE 1 TO WS-A.
           MOVE 2 TO WS-B.
           MOVE 3 TO WS-C.
           GENERATE DET.
           MOVE 10 TO WS-A.
           MOVE 20 TO WS-B.
           MOVE 30 TO WS-C.
           GENERATE DET.
           TERMINATE R.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN WS-BYTE = X"0A"
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = X"0C"
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:16) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
