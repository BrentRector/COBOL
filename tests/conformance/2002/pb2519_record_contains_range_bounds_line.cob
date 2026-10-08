       >>TURN EC-REPORT-COLUMN-OVERLAP CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2519V3.
      *> kb/Work PB2519 - ISO 13.18.43.4 GR18: in Format 3 "integer-4 and integer-5 refer to the minimum number of
      *> bytes in the smallest size record and the maximum number of bytes in the largest size record"; 13.18.12.4
      *> GR2: the CODE characters "are included in the logical record size". The same bound as Format 1 and Format 2:
      *> a report file's record is at most integer-5 bytes, the CODE plus the line.
      *>   cite.py: OK  13.18.14.4 4)   cite.py: OK  13.18.12.4 2)   cite.py: OK  13.18.43.4 6) 7) 18)
      *> DERIVATION. RECORD CONTAINS 1 TO 20: at most 20 bytes. CODE "A" takes one, so the line keeps its first 19
      *> columns: the record is "A0123456789    ABCDE" (Annex A.1 159) latitude for what becomes of the rest,
      *> docs/CONFORMANCE.md DOC-A.1-159).
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB2519V3.TXT".
           SELECT CHK ASSIGN TO "PB2519V3.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT
           RECORD CONTAINS 1 TO 20 CHARACTERS
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
           CODE IS "A"
           PAGE LIMIT IS 10 LINES 40 COLUMNS.
       01  DL TYPE IS DETAIL.
           05 LINE PLUS 1.
              10 COLUMN 1  PIC X(10) VALUE "0123456789".
              10 COLUMN 15  PIC X(10) VALUE "ABCDEFGHIJ".
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
