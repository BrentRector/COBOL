       >>TURN EC-REPORT-PAGE-WIDTH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1059O.
      *> kb/Work PB1059 - ISO 13.18.39.2: integer-2 may be written WITHOUT
      *> integer-1: `PAGE integer-2 {COLS | COLUMNS}` is the second shape of the
      *> brace group (the first being integer-1 alone).
      *>   cite.py: OK  13.18.39.2  (General format)
      *> 13.18.39.3 SR2: "Either integer-1 or integer-2 or both shall be
      *> specified."
      *>   cite.py: OK  13.18.39.3 2)  (Syntax rules)
      *> 13.18.39.4 GR2 a): "If integer-1 is not specified, the report
      *> consists of a single page of indefinite length."
      *>   cite.py: OK  13.18.39.4 2) a)  (General rules)
      *> 13.18.14.4 GR5: past the page width the line is truncated and
      *> EC-REPORT-PAGE-WIDTH is set to exist (cite.py: OK  13.18.14.4 5)).
      *> DERIVATION: `PAGE 8 COLS` gives the report a page width of 8 and NO
      *> page limit, so the report is unpaged: relative LINE clauses only, and
      *> the two detail lines land on consecutive lines of one page. The first
      *> line's item at COLUMN 7 is four wide (ends at 10, past 8): truncated
      *> to columns 7-8, so the line is six spaces then "WX"; the exception is
      *> set. The second line's item at COLUMN 1 is two wide and fits: "OK".
      *> The read-back numbers each physical line and prints its first 10
      *> bytes.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1059O.TXT".
           SELECT CHK ASSIGN TO "PB1059O.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R PAGE 8 COLS.
       01  D1 TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 7 PIC X(4) VALUE "WXYZ".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC XX VALUE "OK".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           GENERATE D1.
           DISPLAY "EC=" FUNCTION EXCEPTION-STATUS.
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
               WHEN WS-BYTE = X"0C"
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   DISPLAY "(page)"
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:10) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
