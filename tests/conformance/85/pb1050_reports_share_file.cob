      *> kb/Work PB1050 - one file description entry naming SEVERAL reports (ISO 13.18.46). `REPORTS ARE R-A R-B` was
      *> refused COBOLNET0899 "multiple reports on one file ... not yet implemented".
      *>
      *> RULES (cite.py --check, all OK):
      *> 13.18.46.4 GR1: "The presence of more than one report-name-1 indicates that more than one report may be
      *> written to the file."
      *> 13.18.46.3 SR1/SR2: each report-name-1 is the subject of a report description entry and appears in only one
      *> REPORT clause (here: both RDs, once each).
      *> 13.18.12.1: the CODE clause "specifies one or more characters used to separate multiple reports written to
      *> the same file", and 13.18.12.4 GR1 puts them in the first characters of each logical record of the report.
      *> 13.18.12.3 SR3: the CODE clause is specified for EACH report of the file (both RDs carry one).
      *>
      *> DERIVATION. Both reports are INITIATEd and TERMINATEd independently and are not divided into pages. A
      *> GENERATE writes one logical record for its detail, led by its report's code: the codes are what separate
      *> the two reports' records on the one file. GENERATE order DE-A (1), DE-B (1), DE-B (2), DE-A (2) therefore
      *> writes, in this order, the records A + "AAA" + blank + "1", B + "BBB" + blank + "1", B + "BBB" + blank +
      *> "2" and A + "AAA" + blank + "2": AAAA 1, BBBB 1, BBBB 2, AAAA 2. Blank lines (each report positions
      *> its own LINE-COUNTER) are not asserted: only the records are, and all-space lines are skipped.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1050RF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1050rf.txt".
           SELECT CHK ASSIGN TO "pb1050rf.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORTS ARE R-A R-B.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-N    PIC 9     VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(12) VALUE SPACES.
       REPORT SECTION.
       RD  R-A CODE IS "A".
       01  DE-A TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "AAA".
           02  COLUMN 5 PIC 9 SOURCE WS-N.
       RD  R-B CODE IS "B".
       01  DE-B TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "BBB".
           02  COLUMN 5 PIC 9 SOURCE WS-N.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RPT.
           INITIATE R-A.
           INITIATE R-B.
           MOVE 1 TO WS-N.
           GENERATE DE-A.
           GENERATE DE-B.
           MOVE 2 TO WS-N.
           GENERATE DE-B.
           GENERATE DE-A.
           TERMINATE R-A.
           TERMINATE R-B.
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
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D
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
