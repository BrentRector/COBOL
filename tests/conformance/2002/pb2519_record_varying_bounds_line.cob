       >>TURN EC-REPORT-COLUMN-OVERLAP CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2519V2.
      *> kb/Work PB2519 - ISO 13.18.43.4 GR7: in Format 2 "Integer-3 specifies the maximum number of bytes in any
      *> record of the file"; 13.18.12.4 GR2: the CODE characters "are included in the logical record size". A report
      *> file's records are therefore at most integer-3 bytes, the CODE plus the line, whichever RECORD format
      *> bounds them (the Format 1 clause, RECORD CONTAINS 20, already did).
      *>   cite.py: OK  13.18.14.4 4)   cite.py: OK  13.18.12.4 2)   cite.py: OK  13.18.43.4 6) 7) 18)
      *> DERIVATION. RECORD IS VARYING IN SIZE FROM 1 TO 20: at most 20 bytes. CODE "A" takes one, so the line
      *> keeps its first 19 columns: "0123456789", four spaces, "ABCDE" (the rest of the line is not recorded,
      *> the Annex A.1 159) latitude, docs/CONFORMANCE.md DOC-A.1-159). The record is "A0123456789    ABCDE".
      *> A build that read only the Format 1 clause wrote the whole 25-character line.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB2519V2.TXT".
           SELECT CHK ASSIGN TO "PB2519V2.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT
           RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS
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
