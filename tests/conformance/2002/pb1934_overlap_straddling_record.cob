       >>TURN EC-REPORT-COLUMN-OVERLAP CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1934ST.
      *> kb/Work PB1934 - ISO 13.18.14.4 GR4 and 13.18.43.4 GR6: an overlap that STRADDLES the end of the record
      *> (RECORD CONTAINS 20; COLUMN 15 X(10) covers columns 15-24, COLUMN 18 X(3) covers 18-20) is raised; the
      *> record keeps columns 1-20 of the line, so the first item is cut after its sixth character (Annex A.1 159)
      *> latitude, docs/CONFORMANCE.md DOC-A.1-159).
      *>   cite.py: OK  13.18.14.4 4)   cite.py: OK  13.18.43.4 6)
      *> DERIVATION. Columns 18-20 are used by both PRESENT WHEN items: the exception exists. "QRS" is not placed
      *> (14.9.16.4 GR8: execution resumes at the next report item), the record is columns 1-20:
      *> "0123456789", four spaces, "ABCDEF".
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1934ST.TXT".
           SELECT CHK ASSIGN TO "PB1934ST.TXT".
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
              10 COLUMN 15  PIC X(10) VALUE "ABCDEFGHIJ"
                 PRESENT WHEN W-FLAG > 0.
              10 COLUMN 18  PIC X(3) VALUE "QRS"
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
