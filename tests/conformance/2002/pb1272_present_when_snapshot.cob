      *> kb/Work PB1272 - PRESENT WHEN is ONE presence snapshot per
      *> report group presentation, taken before any LINE clause is
      *> processed, and a report group whose every LINE is absent is
      *> still PROCESSED (its own sum counters reset) when its level-01
      *> entry is present.
      *>
      *> "If a report group contains any entries that have a PRESENT
      *> WHEN clause, condition-1 of each PRESENT WHEN clause is
      *> evaluated before the processing of any LINE clauses for the
      *> report group"
      *>   cite.py: OK  §13.18.41.4 2)  (General rules)
      *> "Furthermore, if the entry is a level-01 entry, the effect on
      *> processing is as though the entire report group description
      *> were omitted."
      *>   cite.py: OK  §13.18.41.4 2) b)  (General rules)
      *> "the sum counter is reset to zero and the size error indicator
      *> is unset at the end of the processing of the report group in
      *> which it is printed"
      *>   cite.py: OK  §13.18.54.4 2)  (General rules)
      *>
      *> DERIVATION, report R-A (PAGE LIMIT 6, FIRST DETAIL 2, LAST
      *> DETAIL 4). The page heading's USE BEFORE REPORTING declarative
      *> sets WS-F to 1; every GENERATE is preceded by MOVE 0 TO WS-F.
      *> GENERATE 1 prints the page heading first (WS-F = 1), so detail
      *> 1 has no OPT. Details 2 and 3 print OPT on lines 3 and 4. The
      *> 4th detail's condition is evaluated with WS-F = 0 BEFORE its
      *> page fit test (4 + 1 > LAST DETAIL 4) advances the page and
      *> runs the page heading's declarative, so it still prints OPT;
      *> the 5th too. Fails with "4" (no OPT) if the item's condition is
      *> evaluated after the page advance.
      *> DERIVATION, report R-B. CF WS-K's 01 entry is present; its one
      *> LINE is absent while WS-SHOW = 0. CNT (a sum counter with no
      *> COLUMN, printed nowhere, so it resets at the end of CF WS-K)
      *> totals WS-AMT: K=1 AMT 1 -> 1, K=1 AMT 2 -> 3, K=2 AMT 4: the
      *> break processes CF WS-K (nothing prints), CNT resets, then the
      *> GENERATE adds 4 -> 4. WS-SHOW = 1, K=3 AMT 1: BRK prints, CNT
      *> resets, +1 -> 1. Fails with "CNT=07" if a control footing whose
      *> every line is absent skips its end-of-group processing.
      *> Only non-blank lines are displayed, with their page line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1272T.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRA ASSIGN TO "pb1272a.txt".
           SELECT PRB ASSIGN TO "pb1272b.txt".
           SELECT CHA ASSIGN TO "pb1272a.txt".
           SELECT CHB ASSIGN TO "pb1272b.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRA REPORT IS R-A.
       FD  PRB REPORT IS R-B.
       FD  CHA.
       01  CHA-REC PIC X.
       FD  CHB.
       01  CHB-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-F    PIC 9     VALUE 0.
       01  WS-N    PIC 9     VALUE 0.
       01  WS-K    PIC 9     VALUE 1.
       01  WS-AMT  PIC 9     VALUE 0.
       01  WS-SHOW PIC 9     VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-B    PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-A PAGE LIMIT IS 6 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 4.
       01  PH-A TYPE PH.
           02  LINE 1.
               03  COLUMN 1 PIC X(2) VALUE "PH".
       01  DE-A TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC 9 SOURCE WS-N.
               03  COLUMN 3 PIC X(3) VALUE "OPT"
                   PRESENT WHEN WS-F = 0.
       RD  R-B CONTROL IS WS-K PAGE LIMIT IS 20 LINES.
       01  DE-B TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 9 SOURCE WS-K.
       01  CF-B TYPE CF WS-K.
           03  CNT PIC 99 SUM WS-AMT.
           03  LINE PLUS 1 PRESENT WHEN WS-SHOW = 1.
               04  COLUMN 1 PIC X(3) VALUE "BRK".
       PROCEDURE DIVISION.
       DECLARATIVES.
       BR-PH SECTION.
           USE BEFORE REPORTING PH-A.
       BR-PH-P.
           MOVE 1 TO WS-F.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT PRA.
           INITIATE R-A.
           MOVE 1 TO WS-N MOVE 0 TO WS-F GENERATE DE-A.
           MOVE 2 TO WS-N MOVE 0 TO WS-F GENERATE DE-A.
           MOVE 3 TO WS-N MOVE 0 TO WS-F GENERATE DE-A.
           MOVE 4 TO WS-N MOVE 0 TO WS-F GENERATE DE-A.
           MOVE 5 TO WS-N MOVE 0 TO WS-F GENERATE DE-A.
           TERMINATE R-A.
           CLOSE PRA.
           OPEN OUTPUT PRB.
           INITIATE R-B.
           MOVE 1 TO WS-K MOVE 1 TO WS-AMT GENERATE DE-B.
           DISPLAY "CNT=" CNT.
           MOVE 1 TO WS-K MOVE 2 TO WS-AMT GENERATE DE-B.
           DISPLAY "CNT=" CNT.
           MOVE 2 TO WS-K MOVE 4 TO WS-AMT GENERATE DE-B.
           DISPLAY "CNT=" CNT.
           MOVE 1 TO WS-SHOW.
           MOVE 3 TO WS-K MOVE 1 TO WS-AMT GENERATE DE-B.
           DISPLAY "CNT=" CNT.
           TERMINATE R-B.
           CLOSE PRB.
           DISPLAY "R-A".
           OPEN INPUT CHA.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHA
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END MOVE CHA-REC TO WS-B PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHA.
           PERFORM END-FILE.
           DISPLAY "R-B".
           MOVE "N" TO WS-EOF.
           OPEN INPUT CHB.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHB
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END MOVE CHB-REC TO WS-B PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHB.
           PERFORM END-FILE.
           STOP RUN.
       TAKE-BYTE.
           IF WS-B = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF WS-B = X"0C"
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   DISPLAY "(page)"
                   MOVE 0 TO WS-LN
               ELSE
                   IF WS-B NOT = X"0D"
                       ADD 1 TO WS-I
                       MOVE WS-B TO WS-LINE(WS-I:1)
                   END-IF
               END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LINE NOT = SPACES
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:6) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
       END-FILE.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           MOVE 0 TO WS-LN.
