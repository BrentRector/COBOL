       >>TURN EC-REPORT-COLUMN-OVERLAP CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1934OP.
      *> kb/Work PB1934 - ISO 13.18.14.4 GR4: "any given column position is used for only one printable item when
      *> the line is printed. If this rule is violated the EC-REPORT-COLUMN-OVERLAP exception condition is set to
      *> exist". The rule is stated over the report line's column positions (which the page width bounds,
      *> 13.18.39.4 GR2 b)), not over the record: two items that overlap in columns 23 to 24 overlap whether or not
      *> the record (RECORD CONTAINS 20) ends before them. A build that cut the occupancy at the record raised the
      *> exception only when the RECORD CONTAINS value was large enough.
      *>   cite.py: OK  13.18.14.4 4)
      *> The cut itself (columns past integer-1 minus the code are not recorded) is the Annex A.1 159) latitude
      *> docs/CONFORMANCE.md DOC-A.1-159 documents: 13.18.43.4 GR6 sizes each record at integer-1 bytes.
      *> DERIVATION. COLUMN 1 X(10) "0123456789" fills columns 1-10. COLUMN 22 X(3) "XYZ" (columns 22-24) and
      *> COLUMN 23 X(3) "QRS" (columns 23-25) are each subject to a different PRESENT WHEN clause (SR8 a)) and
      *> both hold, so columns 23-24 are used twice: the exception exists. The record holds 20 characters, so the
      *> record is "0123456789" and spaces.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1934OP.TXT".
           SELECT CHK ASSIGN TO "PB1934OP.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT
           RECORD CONTAINS 20 CHARACTERS
           REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       01  W-FLAG  PIC 9     VALUE 1.
       REPORT SECTION.
       RD  R
           PAGE LIMIT IS 10 LINES 40 COLUMNS.
       01  DL TYPE IS DETAIL.
           05 LINE PLUS 1.
              10 COLUMN 1  PIC X(10) VALUE "0123456789".
              10 COLUMN 22  PIC X(3) VALUE "XYZ"
                 PRESENT WHEN W-FLAG > 0.
              10 COLUMN 23  PIC X(3) VALUE "QRS"
                 PRESENT WHEN W-FLAG = 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           GENERATE DL.
           DISPLAY "EXC=[" FUNCTION EXCEPTION-STATUS "]".
           TERMINATE R.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN WS-BYTE = X"0A"
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "RECORD " WS-LN " [" WS-LINE(1:24) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
