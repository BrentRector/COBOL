      *> kb/Work PB1297 - a RESET ON counter resets at the end of its
      *> LEVEL's control footing even when no control footing is
      *> declared for that level; a detail named twice in an UPON
      *> phrase is added twice; and a counter printed in a page footing
      *> resets at the end of that page footing.
      *>
      *> "Subsequently, the sum counter is reset to zero and the size
      *> error indicator is unset at the end of the processing of the
      *> report group in which it is printed or, if the RESET phrase is
      *> specified, at the end of the processing of the control footing
      *> for the specified level of control. If no such control footing
      *> is defined, it is assumed to be present and to consist of a
      *> 01-level entry alone."
      *>   cite.py: OK  §13.18.54.4 2)  (General rules)
      *> "When a GENERATE statement for such a detail is executed, the
      *> adding takes place as many times as data-name-2 appears in the
      *> UPON phrase."
      *>   cite.py: OK  §13.18.54.4 7) c) 2.  (General rules)
      *>
      *> DERIVATION, report R-A (CONTROLS FINAL WS-C, no control footing
      *> declared at all). (C,K) = (1,1) (1,2) (2,3) (2,4). Column 3 is
      *> RESET ON WS-C: the 1 -> 2 break processes the assumed CF WS-C
      *> before the breaking GENERATE's addition, so it restarts: 01 03
      *> 03 07. Column 6 is RESET ON FINAL: 01 03 06 10. Column 9 is
      *> UPON DE-A DE-A with no RESET phrase: each GENERATE adds K
      *> twice, and the counter resets at the end of DE-A, the group it
      *> prints in: 02 04 06 08. Fails with column 3 "06" / "10" on the
      *> last two lines if the WS-C level resets only through a declared
      *> control footing, or with column 9 "01 02 03 04" if a repeated
      *> UPON detail adds once.
      *> DERIVATION, report R-B (PAGE LIMIT 6, LAST DETAIL 3, FOOTING
      *> 5, page footing on line 5 printing SUM WS-P). Each GENERATE
      *> adds 1 before its detail prints; the 4th GENERATE's page fit
      *> test fails after its addition, so page 1's footing shows 04
      *> and the counter resets at the end of that page footing; the
      *> 5th adds 1, so the last page's footing shows 01. Fails with
      *> "PT=05" if a page footing never resets its own counters.
      *> Only non-blank lines are displayed, with their page line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1297T.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRA ASSIGN TO "pb1297a.txt".
           SELECT PRB ASSIGN TO "pb1297b.txt".
           SELECT CHA ASSIGN TO "pb1297a.txt".
           SELECT CHB ASSIGN TO "pb1297b.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRA REPORT IS R-A.
       FD  PRB REPORT IS R-B.
       FD  CHA.
       01  CHA-REC PIC X.
       FD  CHB.
       01  CHB-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-C    PIC 9     VALUE 1.
       01  WS-K    PIC 9     VALUE 0.
       01  WS-P    PIC 9     VALUE 1.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-B    PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-A CONTROLS ARE FINAL WS-C PAGE LIMIT IS 20 LINES.
       01  DE-A TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 9 SOURCE WS-C.
           03  COLUMN 3 PIC 99 SUM WS-K RESET ON WS-C.
           03  COLUMN 6 PIC 99 SUM WS-K RESET ON FINAL.
           03  COLUMN 9 PIC 99 SUM WS-K UPON DE-A DE-A.
       RD  R-B PAGE LIMIT IS 6 LINES HEADING 1 FIRST DETAIL 1
           LAST DETAIL 3 FOOTING 5.
       01  DE-B TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "DET".
       01  PF-B TYPE PF LINE 5.
           03  COLUMN 1 PIC X(3) VALUE "PT=".
           03  COLUMN 4 PIC 99 SUM WS-P.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRA.
           INITIATE R-A.
           MOVE 1 TO WS-C MOVE 1 TO WS-K GENERATE DE-A.
           MOVE 1 TO WS-C MOVE 2 TO WS-K GENERATE DE-A.
           MOVE 2 TO WS-C MOVE 3 TO WS-K GENERATE DE-A.
           MOVE 2 TO WS-C MOVE 4 TO WS-K GENERATE DE-A.
           TERMINATE R-A.
           CLOSE PRA.
           OPEN OUTPUT PRB.
           INITIATE R-B.
           GENERATE DE-B.
           GENERATE DE-B.
           GENERATE DE-B.
           GENERATE DE-B.
           GENERATE DE-B.
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
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:10) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
       END-FILE.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           MOVE 0 TO WS-LN.
