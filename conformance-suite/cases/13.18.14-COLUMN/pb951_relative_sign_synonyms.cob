*>--- conformance-case
*> id: pb951_relative_sign_synonyms
*> rules: [SR-13.18.14.3-2, GR-13.18.14.4-7, GR-13.18.14.4-8, GR-13.18.14.4-9, GR-13.18.14.4-10, SR-13.18.35.3-1, SR-13.18.37.3-2, SR-13.18.37.3-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PLUS AND + ARE ONE OPERATOR IN EVERY REPORT-WRITER RELATIVE OPERAND (kb/Work PB951). The grammar
      *> admitted only the WORD, so `LINE + 1` and `COLUMN + 2` were a bare COBOL0001 "unexpected '+'".
      *>
      *> THE RULES — the same sentence, printed at each clause:
      *> §13.18.35.3 SR1 (LINE)        — "PLUS and + are synonyms."
      *> §13.18.14.3 SR2 (COLUMN)      — "PLUS and + are synonyms."
      *> §13.18.37.3 SR2 (NEXT GROUP)  — "PLUS and + are synonyms."
      *> A synonym is an IDENTITY claim, so DE-W (every relative operand spelled PLUS) and DE-S (every one
      *> spelled +) must print identical lines at identical distances. This golden runs at 2002 because the
      *> relative COLUMN operand is a 2002 addition (construct report-multi-column-2002).
      *>
      *> DERIVATION. The report is not divided into pages (§13.18.39.4 GR2 a)), so §13.18.35.4 GR5 c)
      *> places a body group's first relative line at LINE-COUNTER + integer-2, GR7 b) every later one the
      *> same way, and §13.18.37.4 GR4 b) adds the NEXT GROUP integer-2 to LINE-COUNTER after the group
      *> (an unpaged report has no FOOTING region to clamp it against).
      *> §13.18.14.4 GR7-GR9: the horizontal counter is zero at the start of a line, a relative COLUMN's
      *> leftmost position is the counter + integer-2, and the counter becomes the item's rightmost column.
      *>   GENERATE DE-W: line 0 + 1 = 1: "W" at column 1 (counter 1), then PLUS 2 -> 1 + 2 = 3 "XY";
      *>                  line 1 + 1 = 2: "Z" at column 0 + 2 = 2. NEXT GROUP PLUS 2: LC 2 + 2 = 4.
      *>   GENERATE DE-S: line 4 + 1 = 5 ("S", "XY" at 3), line 6 ("Z" at 2). LC 6 + 2 = 8.
      *>   GENERATE DE-W: line 9 and line 10.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB951RSS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb951rss.txt".
           SELECT CHK ASSIGN TO "pb951rss.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-PS.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-PS.
       01  DE-W TYPE DE NEXT GROUP PLUS 2.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "W".
               03  COLUMN PLUS 2 PIC XX VALUE "XY".
           02  LINE PLUS 1.
               03  COLUMN PLUS 2 PIC X VALUE "Z".
       01  DE-S TYPE DE NEXT GROUP + 2.
           02  LINE + 1.
               03  COLUMN 1 PIC X VALUE "S".
               03  COLUMN + 2 PIC XX VALUE "XY".
           02  LINE + 1.
               03  COLUMN + 2 PIC X VALUE "Z".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-PS.
           GENERATE DE-W.
           GENERATE DE-S.
           GENERATE DE-W.
           TERMINATE R-PS.
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
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:5) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
