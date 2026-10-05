      *> kb/Work PB1221 - every keyword prefix the LINE, COLUMN and
      *> SOURCE formats PRINT is accepted and places its item exactly
      *> as the bare word does (the complement is refused: negatives
      *> pb1221-column-are-singular, pb1221-line-prefix-not-printed
      *> and ReportClauseKeywordPrefixTests).
      *>
      *> LINE (13.18.35.2 Format 1, rendered): the prefix is the brace
      *> {LINE NUMBER IS | LINE NUMBERS ARE | LINES ARE}; NUMBER,
      *> NUMBERS, IS and ARE are not underlined (optional words).
      *>   cite.py: OK  13.18.35.2 (General formats) "NUMBERS"
      *> "LINE and LINES are synonyms."
      *>   cite.py: OK  13.18.35.3 2)  (Syntax rules)
      *> COLUMN (13.18.14.2 Format 1, rendered): {COLUMN NUMBER |
      *> COLUMN NUMBERS | COLUMNS | COL NUMBER | COL NUMBERS | COLS}
      *> {LEFT | CENTER | RIGHT} [IS | ARE], with
      *> "The keyword ARE may be specified only if COLUMNS, COLS, or
      *> NUMBERS is specified."  cite.py: OK  13.18.14.3 4)
      *> "The keyword IS shall not be specified if COLUMNS, COLS, or
      *> NUMBERS is specified."  cite.py: OK  13.18.14.3 5)
      *> SOURCE (13.18.53.2): {SOURCE IS | SOURCES ARE}.
      *> "SOURCE and SOURCES are synonyms."
      *>   cite.py: OK  13.18.53.3 1)  (Syntax rules)
      *> Edition: the LINES / NUMBERS / ARE / COL / COLS / COLUMNS /
      *> SOURCES spellings and the alignment word are COBOL-2002
      *> (COBOL-85 printed only LINE NUMBER IS, COLUMN NUMBER IS and
      *> SOURCE IS), so this golden runs at 2002.
      *>
      *> DERIVATION. The report is not divided into pages, so every
      *> LINE clause is relative; the first line is LINE-COUNTER 0
      *> + 1 (cite.py: OK 13.18.35.4 5) c)), each next one + 1
      *> (cite.py: OK 13.18.35.4 7)).
      *>   L1: COLUMN NUMBERS ARE 1 "A"; COLS ARE 3 "B"; COLUMNS
      *>       RIGHT ARE 6 "CD" - RIGHT: leftmost = 6 - 2 + 1 = 5;
      *>       COL NUMBERS LEFT ARE 8 "E"; COL NUMBER CENTER IS 11
      *>       "FGH" - CENTER, odd size 3: leftmost = 11 - 1 = 10;
      *>       COLUMN IS 14 "I"; COL NUMBER 16 "J" (cite.py: OK
      *>       13.18.14.4 6) c) and 6) d)) -> "A B CD E FGH I J".
      *>   L2: SOURCES ARE WS-A at 1, SOURCE IS WS-B at 5
      *>       -> "XYZ UVW".
      *>   L3..L8: LINE ARE, LINE IS, LINE NUMBER, LINE NUMBER IS,
      *>       LINES, LINE NUMBERS, one letter each at columns 2..7
      *>       (COLUMN NUMBER IS, COLUMN, COLUMNS, COLS, COLUMN,
      *>       COLUMN) -> " K", "  L", "   M", "    N", "     O",
      *>       "      P".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1221P.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1221p.txt".
           SELECT CHK ASSIGN TO "pb1221p.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-K.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-A    PIC X(3)  VALUE "XYZ".
       01  WS-B    PIC X(3)  VALUE "UVW".
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-K.
       01  DET1 TYPE DE.
           02  LINE NUMBERS ARE PLUS 1.
               03  COLUMN NUMBERS ARE 1 PIC X VALUE "A".
               03  COLS ARE 3 PIC X VALUE "B".
               03  COLUMNS RIGHT ARE 6 PIC XX VALUE "CD".
               03  COL NUMBERS LEFT ARE 8 PIC X VALUE "E".
               03  COL NUMBER CENTER IS 11 PIC XXX VALUE "FGH".
               03  COLUMN IS 14 PIC X VALUE "I".
               03  COL NUMBER 16 PIC X VALUE "J".
           02  LINES ARE PLUS 1.
               03  COLUMN 1 PIC X(3) SOURCES ARE WS-A.
               03  COLUMN 5 PIC X(3) SOURCE IS WS-B.
           02  LINE ARE PLUS 1.
               03  COLUMN NUMBER IS 2 PIC X VALUE "K".
           02  LINE IS PLUS 1.
               03  COLUMN 3 PIC X VALUE "L".
           02  LINE NUMBER PLUS 1.
               03  COLUMNS 4 PIC X VALUE "M".
           02  LINE NUMBER IS PLUS 1.
               03  COLS 5 PIC X VALUE "N".
           02  LINES PLUS 1.
               03  COLUMN 6 PIC X VALUE "O".
           02  LINE NUMBERS PLUS 1.
               03  COLUMN 7 PIC X VALUE "P".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-K.
           GENERATE DET1.
           TERMINATE R-K.
           CLOSE PRT.
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
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:16) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
