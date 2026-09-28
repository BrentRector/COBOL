       >>TURN EC-REPORT-LINE-OVERLAP CHECKING ON
      *> kb/Work PB1130 - the report engine's two missing exception
      *> mechanisms: a SUM counter's size error indicator, and a
      *> nonfatal report exception that GENERATE raises and resumes
      *> after.
      *>
      *> R-S - THE SIZE ERROR INDICATOR (checking OFF for SUM-SIZE).
      *> "Each entry containing a SUM clause establishes an independent
      *> sum counter and size error indicator."
      *>   cite.py: OK  §13.18.54.4 1)  (General rules)
      *> "Each addition is tested for size error"
      *>   cite.py: OK  §13.18.54.4 3)  (General rules)
      *> "If the associated size error indicator is set, an
      *> EC-REPORT-SUM-SIZE exception condition is set to exist and the
      *> printable item is filled with spaces"
      *>   cite.py: OK  §13.18.54.4 4)  (General rules)
      *> "All sum counters and all size error indicators are set to
      *> zero"   (INITIATE)
      *>   cite.py: OK  §14.9.21.4 1) a)  (General rules)
      *> DERIVATION. The counter has the two digits of PIC 99 (GR1).
      *> The first GENERATE adds 60; the second adds 60 again, and 120
      *> does not fit: a size error, so the counter keeps 60 and its
      *> indicator is set. TERMINATE prints CF FINAL with the indicator
      *> set, so the printable item is SPACES: "S=  " (the defect
      *> printed the high-order-truncated "S=20"). The file is reopened
      *> EXTEND, and the second INITIATE unsets the indicator and zeroes the counter, so one GENERATE
      *> of 5 prints "S=05".
      *>
      *> R-O - A NONFATAL EXCEPTION RAISED BY GENERATE, AND ITS RESUME.
      *> "If this rule is violated the EC-REPORT-LINE-OVERLAP exception
      *> condition is set to exist and the results are undefined."
      *>   cite.py: OK  §13.18.35.4 3)  (General rules)
      *> "execution resumes at the next report item, line, or report
      *> group"   (GENERATE)
      *>   cite.py: OK  §14.9.16.4 8)  (General rules)
      *> "If any two or more absolute lines are defined using line
      *> numbers that are not in increasing numerical order, they shall
      *> each be subject to a different PRESENT WHEN clause."
      *>   cite.py: OK  §13.18.35.3 6) a)  (Syntax rules)
      *> DERIVATION. DET-O's LINE 5 and LINE 3 are legal (each has its
      *> own PRESENT WHEN), and with WS-K = 2 both are present, so the
      *> group violates GR3 at run time. "O5" prints on line 5 (the
      *> first body group on the page, absolute). LINE 3 is above the
      *> line already printed: EC-REPORT-LINE-OVERLAP is raised (its
      *> checking is ON), the declarative runs ("OVERLAP" + the
      *> EXCEPTION-STATUS name), and GENERATE resumes at the next line
      *> - there is none in DET-O, so "O3" is never printed, and
      *> LINE-COUNTER still names line 5, the last line printed
      *> (§13.18.35.4 GR1). DET-P (LINE PLUS 1) passes the page fit
      *> test and prints "P" on line 6. The defect raised nothing,
      *> pushed "O3" down to line 6, and "P" to line 7.
      *> Only non-blank lines are displayed, with their line number.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1130SO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRS ASSIGN TO "pb1130s.txt".
           SELECT PRO ASSIGN TO "pb1130o.txt".
           SELECT CHS ASSIGN TO "pb1130s.txt".
           SELECT CHO ASSIGN TO "pb1130o.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRS REPORT IS R-S.
       FD  PRO REPORT IS R-O.
       FD  CHS.
       01  CHS-REC PIC X.
       FD  CHO.
       01  CHO-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-N    PIC 99    VALUE 60.
       01  WS-K    PIC 9     VALUE 1.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-C    PIC X     VALUE SPACE.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-S CONTROL IS FINAL.
       01  DET-S TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE WS-N.
       01  TYPE CF FINAL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "S=".
           02  COLUMN 3 PIC 99 SUM WS-N.
       RD  R-O PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 15 FOOTING 18.
       01  DET-O TYPE DE.
           02  LINE 5 PRESENT WHEN WS-K > 0.
               03  COLUMN 1 PIC X(2) VALUE "O5".
           02  LINE 3 PRESENT WHEN WS-K > 1.
               03  COLUMN 1 PIC X(2) VALUE "O3".
       01  DET-P TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X VALUE "P".
       PROCEDURE DIVISION.
       DECLARATIVES.
       OV SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-LINE-OVERLAP.
       OV-P.
           DISPLAY "OVERLAP " FUNCTION EXCEPTION-STATUS.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT PRS.
           INITIATE R-S.
           GENERATE DET-S.
           GENERATE DET-S.
           TERMINATE R-S.
           CLOSE PRS.
           OPEN EXTEND PRS.
           MOVE 5 TO WS-N.
           INITIATE R-S.
           GENERATE DET-S.
           TERMINATE R-S.
           CLOSE PRS.
           OPEN OUTPUT PRO.
           INITIATE R-O.
           MOVE 2 TO WS-K.
           GENERATE DET-O.
           GENERATE DET-P.
           TERMINATE R-O.
           CLOSE PRO.
           DISPLAY "R-S".
           OPEN INPUT CHS.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHS
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END MOVE CHS-REC TO WS-C
                              PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHS.
           PERFORM END-FILE.
           DISPLAY "R-O".
           MOVE "N" TO WS-EOF.
           OPEN INPUT CHO.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHO
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END MOVE CHO-REC TO WS-C
                              PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHO.
           PERFORM END-FILE.
           STOP RUN.
       TAKE-BYTE.
           IF WS-C = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF WS-C NOT = X"0D" AND WS-C NOT = X"0C"
                   ADD 1 TO WS-I
                   MOVE WS-C TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       END-FILE.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           MOVE 0 TO WS-LN.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LINE NOT = SPACES
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:4) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
