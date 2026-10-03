       >>TURN EC-REPORT-PAGE-WIDTH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1059W.
      *> kb/Work PB1059 - ISO 13.18.39.2: the PAGE clause's second operand
      *> is the page width,
      *>   PAGE [LIMIT IS | LIMITS ARE] { integer-1 |
      *>        [integer-1 {LINE | LINES}] [integer-2 {COLS | COLUMNS}] } ...
      *>   cite.py: OK  13.18.39.2  (General format)
      *> 13.18.39.4 GR2 b): "Integer-2 is the page width. It defines the
      *> maximum number of print columns that may be accommodated in any
      *> line of the report."
      *>   cite.py: OK  13.18.39.4 2) b)  (General rules)
      *> 13.18.14.4 GR5: "Any report line shall be defined in such a way that,
      *> when printed, the final column position of the last printable item
      *> does not exceed the page width. If this rule is violated the
      *> EC-REPORT-PAGE-WIDTH exception condition is set to exist, the report
      *> line is truncated, and the report line is printed."
      *>   cite.py: OK  13.18.14.4 5)  (General rules)
      *> Before the surface existed the width was the 999 of 13.18.39.4 GR5
      *> whatever the program wrote, and `10 COLUMNS` was a parse error.
      *> DERIVATION: PAGE LIMIT IS 10 LINES 12 COLUMNS, so the page width is
      *> 12. The item at COLUMN 10 is five wide and ends at column 14, past
      *> 12: the exception is set, the line is truncated at column 12 and
      *> printed, so columns 10-12 hold "ABC" and the line is "OK", seven
      *> spaces, then "ABC". EXCEPTION-STATUS then names EC-REPORT-PAGE-WIDTH.
      *> The item at COLUMN 1 ("OK") ends at column 2 and is untouched.
      *> The read-back numbers each physical line and prints its first 14
      *> bytes.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1059W.TXT".
           SELECT CHK ASSIGN TO "PB1059W.TXT".
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
       RD  R PAGE LIMIT IS 10 LINES 12 COLUMNS.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "OK".
           03  COLUMN 10 PIC X(5) VALUE "ABCDE".
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
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:14) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
