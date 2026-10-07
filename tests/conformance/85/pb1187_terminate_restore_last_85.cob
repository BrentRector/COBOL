      *> kb/Work PB1187 - TERMINATE restores the control data items
      *> LAST: the final page footing and the report footing are
      *> composed while the control items still hold their PRIOR values.
      *>
      *> "a) The contents of any control data items are changed to
      *> their prior values."            (TERMINATE GR3, the bracket)
      *> "c) The report footing is printed, if defined."
      *>   cite.py: OK  §14.9.46.4 3) c)  (General rules)
      *> "d) The contents of any control data items are restored to the
      *> values they had at the start of execution of the TERMINATE
      *> statement."
      *>   cite.py: OK  §14.9.46.4 3) d)  (General rules)
      *> "If a report footing is defined and is not on a page by itself,
      *> the page footing on the last page is immediately followed by
      *> the report footing."
      *>   cite.py: OK  §13.18.57.4 6) f) 2.  (General rules)
      *> "When a TERMINATE statement is executed, if any control footing
      *> is defined for the report, the prior controls are stored in the
      *> control data items before each control footing is printed"
      *>   cite.py: OK  §13.18.16.4 5)  (General rules)
      *>
      *> DERIVATION. The one GENERATE runs with WS-A = 1, WS-B = 1, so
      *> both prior controls hold 1. The program then moves 3 and 4 into
      *> the control items and executes TERMINATE: a) puts 1 and 1 back,
      *> b) prints CF WS-B then CF WS-A (1 1), the last page's page
      *> footing prints (FOOTING 18 + 1 = line 19) and c) the report
      *> footing follows it (LINE-COUNTER 19 + 1 = line 20) - all four
      *> BEFORE d), so all four show 1 1. d) then restores 3 and 4, which
      *> the DISPLAY after TERMINATE shows.
      *> Fails if d) runs before the page footing / report footing (the
      *> defect: "PF 3 4" / "RF 3 4"), or never runs ("AFTER 1 1").
      *> Placement: FIRST DETAIL 2 (the first body group on the page,
      *> §13.18.35.4 GR5 b) 3.), then LINE PLUS 1 per group.
      *> Only non-blank lines are displayed, with their line number.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1187T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D SYM-X0C
               ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1187t.txt".
           SELECT CHK ASSIGN TO "pb1187t.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-A    PIC 9     VALUE 1.
       01  WS-B    PIC 9     VALUE 1.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 CONTROLS ARE FINAL WS-A WS-B
           PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 15 FOOTING 18.
       01  DE-1 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "DE".
           02  COLUMN 5 PIC 9 SOURCE WS-A.
           02  COLUMN 7 PIC 9 SOURCE WS-B.
       01  TYPE CF WS-B LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CFB".
           02  COLUMN 5 PIC 9 SOURCE WS-A.
           02  COLUMN 7 PIC 9 SOURCE WS-B.
       01  TYPE CF WS-A LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CFA".
           02  COLUMN 5 PIC 9 SOURCE WS-A.
           02  COLUMN 7 PIC 9 SOURCE WS-B.
       01  TYPE PF LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "PF".
           02  COLUMN 5 PIC 9 SOURCE WS-A.
           02  COLUMN 7 PIC 9 SOURCE WS-B.
       01  TYPE RF LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "RF".
           02  COLUMN 5 PIC 9 SOURCE WS-A.
           02  COLUMN 7 PIC 9 SOURCE WS-B.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DE-1.
           MOVE 3 TO WS-A.
           MOVE 4 TO WS-B.
           TERMINATE R-1.
           DISPLAY "AFTER " WS-A " " WS-B.
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
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D AND CHK-REC NOT = SYM-X0C
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LINE NOT = SPACES
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:7) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
