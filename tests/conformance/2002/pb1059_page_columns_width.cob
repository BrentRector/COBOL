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
      *> 13.18.14.3 SR8 b) bars an ABSOLUTE item from ending past the page
      *> width, and SR8 c) bars relative items at the end of a line from
      *> causing it "unless each of them is subject to a different PRESENT
      *> WHEN clause, in which case this rule applies only to the largest of
      *> them".   cite.py: OK  13.18.14.3 8) c)  (Syntax rules)
      *> So the exception is reached by two relative items under DIFFERENT
      *> PRESENT WHEN clauses, each of which fits alone and which together do
      *> not (the clauses are different; nothing says they exclude each other).
      *> DERIVATION: PAGE LIMIT IS 10 LINES 12 COLUMNS, so the page width is
      *> 12. Item A is COLUMN PLUS 1 and six wide: GR8 starts the counter at
      *> zero, so it occupies columns 1-6. Item B is COLUMN PLUS 3 and five
      *> wide: the counter is 6, so it starts at column 9 and ends at 13, past
      *> 12 (alone it would start at 3 and end at 7). Both conditions hold,
      *> so the exception is set, the line is truncated at column 12 and
      *> printed: columns 9-12 hold "VWXY" and the line is "ABCDEF", two
      *> spaces, then "VWXY". EXCEPTION-STATUS then names EC-REPORT-PAGE-WIDTH.
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
       01  WS-ON   PIC 9     VALUE 1.
       01  WS-TWO  PIC 9     VALUE 1.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R PAGE LIMIT IS 10 LINES 12 COLUMNS.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN PLUS 1 PIC X(6) VALUE "ABCDEF"
               PRESENT WHEN WS-ON = 1.
           03  COLUMN PLUS 3 PIC X(5) VALUE "VWXYZ"
               PRESENT WHEN WS-TWO = 1.
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
