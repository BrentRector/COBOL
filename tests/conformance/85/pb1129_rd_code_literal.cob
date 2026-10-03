      *> kb/Work PB1129 - the RD CODE clause (ISO 13.18.12), literal form. It was refused COBOLNET0899 in every form.
      *>
      *> RULES (cite.py --check, all OK):
      *> 13.18.12.4 GR1: "When the CODE clause is specified, literal-1 or identifier-1 is automatically placed in
      *> the first characters of each logical record written to the report file for this report."
      *> 13.18.12.4 GR2: "The characters occupied by literal-1 or identifier-1 are not included in the
      *> descriptions of the lines in the report, but are included in the logical record size."
      *> 13.18.12.3 SR1: "Literal-1 shall be an alphanumeric literal."
      *>
      *> DERIVATION. CODE IS "A1" puts A1 at positions 1-2 of every record, and the line description does NOT
      *> count it: COLUMN 1 is the first character AFTER the code, so "HELLO" occupies record positions 3-7,
      *> position 8 (line column 6) is blank, and the digit of COLUMN 7 stands at record position 9. Two GENERATEs
      *> with WS-N 1 then 2 give the two records A1HELLO 1 and A1HELLO 2. The report has no PAGE clause
      *> (13.18.39.4 GR2 a)), so only the printed lines are asserted; all-space lines are skipped.
      *> GR2 ("... but are included in the logical record size"): the FD says RECORD CONTAINS 10 CHARACTERS, so the
      *> record is the 2 code characters PLUS an 8-column line. The third item, COLUMN 9 "ZZ", would stand in
      *> columns 9-10 of the line - record positions 11-12, past the record - so it does not print at all. An
      *> engine that counted the code outside the record would print A1HELLO 1 ZZ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1129L.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1129l.txt".
           SELECT CHK ASSIGN TO "pb1129l.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-A RECORD CONTAINS 10 CHARACTERS.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-N    PIC 9     VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(12) VALUE SPACES.
       REPORT SECTION.
       RD  R-A CODE IS "A1".
       01  DE-A TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(5) VALUE "HELLO".
           02  COLUMN 7 PIC 9 SOURCE WS-N.
           02  COLUMN 9 PIC XX VALUE "ZZ".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RPT.
           INITIATE R-A.
           MOVE 1 TO WS-N.
           GENERATE DE-A.
           MOVE 2 TO WS-N.
           GENERATE DE-A.
           TERMINATE R-A.
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
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D"
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
