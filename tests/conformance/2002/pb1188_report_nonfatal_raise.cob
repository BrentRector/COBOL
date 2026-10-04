       >>TURN EC-REPORT-COLUMN-OVERLAP CHECKING ON
       >>TURN EC-REPORT-PAGE-LIMIT CHECKING ON
       >>TURN EC-REPORT-PAGE-WIDTH CHECKING ON
      *> kb/Work PB1188 - the report writer's nonfatal exception
      *> conditions EC-REPORT-COLUMN-OVERLAP, EC-REPORT-PAGE-LIMIT and
      *> EC-REPORT-PAGE-WIDTH are raised where the rules are broken, and
      *> GENERATE / TERMINATE resume where the standard says.
      *>
      *> "If this rule is violated the EC-REPORT-COLUMN-OVERLAP
      *> exception condition is set to exist and the results are
      *> undefined."
      *>   cite.py: OK  §13.18.14.4 4)  (General rules)
      *> "If this rule is violated the EC-REPORT-PAGE-WIDTH exception
      *> condition is set to exist, the report line is truncated, and
      *> the report line is printed."
      *>   cite.py: OK  §13.18.14.4 5)  (General rules)
      *> "A report group shall never be split between two pages. If
      *> this rule is violated the EC-REPORT-PAGE-LIMIT exception
      *> condition is set to exist and the results are undefined."
      *>   cite.py: OK  §13.18.35.4 2)  (General rules)
      *> "If a nonfatal exception condition is raised during the
      *> execution of a GENERATE statement, execution resumes at the
      *> next report item, line, or report group, whichever follows in
      *> logical order."
      *>   cite.py: OK  §14.9.16.4 8)  (General rules)
      *> "If a nonfatal exception condition is raised during the
      *> execution of a TERMINATE statement, execution resumes at the
      *> next report item, line, or report group, whichever follows in
      *> logical order."
      *>   cite.py: OK  §14.9.46.4 5)  (General rules)
      *> "If integer-2 is omitted, a value of 999 is assumed for the
      *> page width."
      *>   cite.py: OK  §13.18.39.4 5)  (General rules)
      *>
      *> The two rules the programs below break at run time are also syntax
      *> rules (§13.18.14.3 SR8 a), b), c); cite.py: OK 13.18.14.3 8)), each
      *> excusing items "subject to a different PRESENT WHEN clause" - and
      *> different clauses may both hold, which is how a legal program
      *> reaches the exception: R-C's overlapping items and R-W's two
      *> relative items carry different clauses and both are present.
      *>
      *> DERIVATION. R-C: "B" at column 3 falls inside "AAA" (columns
      *> 2-4): the declarative runs and GENERATE resumes at the next
      *> item, so the line is " AAA C". R-L (PAGE LIMIT 6, FIRST DETAIL
      *> 2): DE-L is the first body group, so no page fit test; its six
      *> lines go to lines 2..7 and line 7 is past the page limit - the
      *> declarative runs and GENERATE resumes at the next report group,
      *> so L6 is never printed. DE-S then fails its page fit test and
      *> lands on the next page at FIRST DETAIL 2. At TERMINATE the
      *> CONTROL FOOTING FINAL (three lines from LINE-COUNTER 2, trial
      *> 2 + 3 = 5 <= FOOTING 5, so it fits) goes to lines 3..5; the
      *> page footing is absent; the report footing on its own page
      *> prints "RF" at line 1, so TERMINATE got that far. R-T: the
      *> control footing FINAL is eight lines, placed from FIRST DETAIL
      *> 1 after its failed page fit test: line 7 raises, and TERMINATE
      *> resumes at the next group - the report footing, on its own
      *> page. R-W: item W (COLUMN PLUS 1) is column 1; item "12345"
      *> (COLUMN PLUS 995, five wide) alone would be columns 995-999 and fits
      *> the page width 999, but after W the counter is 1 so it starts at 996
      *> and ends at 1000, past 999: the declarative runs and the line is
      *> truncated at 999 and printed (tail "1234 ", length 999).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1188T.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRC ASSIGN TO "pb1188c.txt".
           SELECT PRL ASSIGN TO "pb1188l.txt".
           SELECT PRT ASSIGN TO "pb1188t.txt".
           SELECT PRW ASSIGN TO "pb1188w.txt".
           SELECT CHC ASSIGN TO "pb1188c.txt".
           SELECT CHL ASSIGN TO "pb1188l.txt".
           SELECT CHT ASSIGN TO "pb1188t.txt".
           SELECT CHW ASSIGN TO "pb1188w.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRC REPORT IS R-C.
       FD  PRL REPORT IS R-L.
       FD  PRT REPORT IS R-T.
       FD  PRW REPORT IS R-W.
       FD  CHC.
       01  CHC-REC PIC X.
       FD  CHL.
       01  CHL-REC PIC X.
       FD  CHT.
       01  CHT-REC PIC X.
       FD  CHW.
       01  CHW-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-ON   PIC 9     VALUE 1.
       01  WS-TWO  PIC 9     VALUE 1.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-B    PIC X.
       01  WS-I    PIC 9(4)  VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(1000) VALUE SPACES.
       REPORT SECTION.
       RD  R-C.
       01  DE-C TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 2 PIC XXX VALUE "AAA" PRESENT WHEN WS-ON = 1.
               03  COLUMN 3 PIC X VALUE "B" PRESENT WHEN WS-TWO = 1.
               03  COLUMN 6 PIC X VALUE "C".
       RD  R-L CONTROL IS FINAL
           PAGE LIMIT 6 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 5 FOOTING 5.
       01  DE-L TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "L1".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "L2".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "L3".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "L4".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "L5".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "L6".
       01  DE-S TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "S1".
       01  CF-L TYPE CF FINAL.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "C1".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "C2".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "C3".
       01  RF-L TYPE RF LINE NEXT PAGE.
           03  COLUMN 1 PIC X(2) VALUE "RF".
       RD  R-T CONTROL IS FINAL
           PAGE LIMIT 6 LINES HEADING 1 FIRST DETAIL 1
           LAST DETAIL 6 FOOTING 6.
       01  DE-T TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "D1".
       01  CF-T TYPE CF FINAL.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "F1".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "F2".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "F3".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "F4".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "F5".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "F6".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "F7".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "F8".
       01  RF-T TYPE RF LINE NEXT PAGE.
           03  COLUMN 1 PIC X(2) VALUE "RT".
       RD  R-W.
       01  DE-W TYPE DE LINE PLUS 1.
           03  COLUMN PLUS 1 PIC X VALUE "W" PRESENT WHEN WS-ON = 1.
           03  COLUMN PLUS 995 PIC X(5) VALUE "12345"
               PRESENT WHEN WS-TWO = 1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       NF SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-COLUMN-OVERLAP
               EC-REPORT-PAGE-LIMIT EC-REPORT-PAGE-WIDTH.
       NF-P.
           DISPLAY "EC " FUNCTION EXCEPTION-STATUS.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT PRC PRL PRT PRW.
           INITIATE R-C R-L R-T R-W.
           DISPLAY "GENERATE DE-C".
           GENERATE DE-C.
           DISPLAY "GENERATE DE-L".
           GENERATE DE-L.
           DISPLAY "GENERATE DE-S".
           GENERATE DE-S.
           DISPLAY "GENERATE DE-T".
           GENERATE DE-T.
           DISPLAY "GENERATE DE-W".
           GENERATE DE-W.
           DISPLAY "TERMINATE R-L".
           TERMINATE R-L.
           DISPLAY "TERMINATE R-T".
           TERMINATE R-T.
           TERMINATE R-C R-W.
           CLOSE PRC PRL PRT PRW.
           DISPLAY "R-C".
           OPEN INPUT CHC.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHC
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END MOVE CHC-REC TO WS-B PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHC.
           PERFORM END-FILE.
           DISPLAY "R-L".
           OPEN INPUT CHL.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHL
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END MOVE CHL-REC TO WS-B PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHL.
           PERFORM END-FILE.
           DISPLAY "R-T".
           OPEN INPUT CHT.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHT
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END MOVE CHT-REC TO WS-B PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHT.
           PERFORM END-FILE.
           DISPLAY "R-W".
           OPEN INPUT CHW.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHW
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END MOVE CHW-REC TO WS-B PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHW.
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
               IF WS-I > 900
                   DISPLAY "  TAIL [" WS-LINE(996:5) "] LENGTH " WS-I
               END-IF
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
       END-FILE.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           MOVE 0 TO WS-LN.
           MOVE "N" TO WS-EOF.
