      *> kb/Work PB1222 - the PRESENT WHEN escape of ISO 13.18.14.3 SR7/SR8 a) and 13.18.35.3 SR6 a)-e): items and lines that
      *> break the arrangement rules are legal when each is "subject to a different PRESENT WHEN clause". PRESENT WHEN is a
      *> COBOL 2002 clause (13.18.41), so this is the introducing-edition witness; conformance:negative/pb1222-* are the refused side
      *> (the same arrangements with no clause, all four editions).
      *> SR7: "any two or more absolute items defined using column numbers that are not in increasing numerical order shall be
      *>   subject to a different PRESENT WHEN clause."   cite.py: OK  13.18.14.3 7)  (Syntax rules)
      *> SR8 a): "If any two or more items overlap each other, they shall each be subject to a different PRESENT WHEN clause."
      *>   cite.py: OK  13.18.14.3 8) a)  (Syntax rules)
      *> SR10 b): "All the occurrences of integer-1 shall be in increasing order of magnitude."   cite.py: OK  13.18.14.3 10) b)
      *> SR6 a): "If any two or more absolute lines are defined using line numbers that are not in increasing numerical order, they
      *>   shall each be subject to a different PRESENT WHEN clause."   cite.py: OK  13.18.35.3 6) a)  (Syntax rules)
      *> SR6 e): "If the description of any absolute line appears later than that of a relative line, they shall each be subject to a
      *>   different PRESENT WHEN clause."   cite.py: OK  13.18.35.3 6) e)  (Syntax rules)
      *> DERIVATION. PAGE LIMIT 10, HEADING 1, FIRST DETAIL 3, LAST DETAIL 6, FOOTING 6 (LAST DETAIL = FOOTING, equality allowed).
      *> W1 = 1 (true) and W2 = 0 (false). D1 has four lines, each under its own PRESENT WHEN clause: LINE PLUS 1 (W2: absent), then
      *> LINE 5 (W1: present), LINE 4 (W2: absent) and LINE 6 (W1: present) - the absolute lines are out of order and described after
      *> a relative line, which the different clauses permit; all are within the detail's limits 3 and 6 (13.18.57.4 GR7 c, GR8 e).
      *> On line 5 the items are A at COLUMN 20 (W1), B at COLUMN 10 (W1), C at COLUMN 1, twelve wide (W2) and M at COLUMN 25 30: A
      *> and B are out of order, C overlaps B and is out of order with both, each under a different clause; the multiple COLUMN
      *> clause is in increasing order and, being a repeating entry, prints M at 25 and at 30 (13.15.4 GR3). C is absent. The page
      *> heading PH on line 2 is ON its lower limit, FIRST DETAIL - 1 = 2 (GR8 c); the page footing PF on line 7 is ON its upper
      *> limit, FOOTING + 1 = 7 (GR7 e).
      *> D2 is never generated: it is here for the compiler to accept (13.18.35.3 SR6 d: an unconditional absolute LINE 4, then
      *> two relative lines under different clauses - together they would reach line 7, past the lower limit 6, but "this rule
      *> applies only to the largest of them": LINE PLUS 2 alone is line 6, on the limit).
      *> Line 5: columns 1-9 blank, B in 10, blank 11-19, A in 20, blank 21-24, M in 25, blank 26-29, M in 30. Line 6: "L6".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1222P.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1222P.TXT".
           SELECT CHK ASSIGN TO "PB1222P.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  W1 PIC 9 VALUE 1.
       01  W2 PIC 9 VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 6 FOOTING 6.
       01  PH1 TYPE PH LINE 2.
           03  COLUMN 1 PIC X(2) VALUE "PH".
       01  D1 TYPE DE.
           03  LINE PLUS 1 PRESENT WHEN W2 = 1.
               05  COLUMN 1 PIC X VALUE "R".
           03  LINE 5 PRESENT WHEN W1 = 1.
               05  COLUMN 20 PIC X VALUE "A" PRESENT WHEN W1 = 1.
               05  COLUMN 10 PIC X VALUE "B" PRESENT WHEN W1 = 1.
               05  COLUMN 1 PIC X(12) VALUE "CCCCCCCCCCCC"
                   PRESENT WHEN W2 = 1.
               05  COLUMN 25 30 PIC X VALUE "M".
           03  LINE 4 PRESENT WHEN W2 = 1.
               05  COLUMN 1 PIC X VALUE "X".
           03  LINE 6 PRESENT WHEN W1 = 1.
               05  COLUMN 1 PIC X(2) VALUE "L6".
       01  D2 TYPE DE.
           03  LINE 4.
               05  COLUMN 1 PIC X VALUE "P".
           03  LINE PLUS 1 PRESENT WHEN W1 = 1.
               05  COLUMN 1 PIC X VALUE "Q".
           03  LINE PLUS 2 PRESENT WHEN W2 = 1.
               05  COLUMN 1 PIC X VALUE "S".
       01  PF1 TYPE PF LINE 7.
           03  COLUMN 1 PIC X(2) VALUE "PF".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
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
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LINE NOT = SPACES
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:30) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
