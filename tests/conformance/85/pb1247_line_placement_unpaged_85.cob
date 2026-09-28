      *> kb/Work PB1247 - run-time line placement: a relative line with an
      *> integer-2 of zero OVERWRITES the preceding line, and a report
      *> NOT divided into pages places its report heading and report
      *> footing at LINE-COUNTER + integer-2.
      *>
      *> "Each report group shall be described such that, when it is
      *> printed, no lines or groups of lines overlap each other, except
      *> that the non-space characters of a relative line specified with
      *> an integer-2 of zero will overwrite the corresponding characters
      *> of the preceding line."
      *>   cite.py: OK  §13.18.35.4 3)  (General rules)
      *> "If the first LINE NUMBER clause of the report group is relative
      *> and the report is not divided into pages, the report group's
      *> first line number is obtained by adding integer-2 to the current
      *> value of the report's LINE-COUNTER."
      *>   cite.py: OK  §13.18.35.4 5) 5. c)  (General rules)
      *> "The report's LINE-COUNTER identifier contains the line number
      *> within the page of the most recent line to have been printed."
      *>   cite.py: OK  §13.18.35.4 1)  (General rules)
      *>
      *> DERIVATION (RD with no PAGE clause - one page of indefinite
      *> length; LINE-COUNTER 0 after INITIATE):
      *>   RH LINE PLUS 3              -> 0 + 3 = line 3  "RHEAD"
      *>   DET (x2): LINE PLUS 1 "ABC" in column 1, then LINE PLUS 0
      *>     "XYZ" in column 5 - the same line, the two merged:
      *>                               -> lines 4 and 5  "ABC XYZ"
      *>   RF LINE PLUS 2              -> 5 + 2 = line 7  "RFOOT"
      *>   Lines 1, 2 and 6 are blank. LINE-COUNTER after the first
      *>   GENERATE is 4 (the line the LINE PLUS 0 overwrote).
      *> Fails if the zero advance is forced to one line ("    XYZ" on a
      *> line of its own, and every later line one lower), or if the
      *> unpaged RH / RF take the paged formulas (the defect: RHEAD on
      *> line 2, HEADING + integer-2 - 1; RFOOT pushed below the body).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1247L.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1247l.txt".
           SELECT CHK ASSIGN TO "pb1247l.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-LC   PIC 99    VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(10) VALUE SPACES.
       REPORT SECTION.
       RD  R-1.
       01  RH-1 TYPE RH LINE PLUS 3.
           02  COLUMN 1 PIC X(5) VALUE "RHEAD".
       01  DET-1 TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(3) VALUE "ABC".
           02  LINE PLUS 0.
               03  COLUMN 5 PIC X(3) VALUE "XYZ".
       01  RF-1 TYPE RF LINE PLUS 2.
           02  COLUMN 1 PIC X(5) VALUE "RFOOT".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DET-1.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC " WS-LC.
           GENERATE DET-1.
           TERMINATE R-1.
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
               IF CHK-REC NOT = X"0D" AND CHK-REC NOT = X"0C"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:7) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
