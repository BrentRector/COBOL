      *>--- conformance-case
      *> id: l1c16_report_lines_synonym
      *> rules: [SR-13.18.35.3-2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.35.3 SR2 — LINES is a synonym of LINE in the
      *>   report-writer
      *> LINE clause (single absolute, relative, and multiple operands)
      *> SR2 "LINE and LINES are synonyms."
      *>   cite.py: OK  §13.18.35.3 2)  (Syntax rules)
      *> Every LINE clause below is spelled LINES, so each must behave
      *>   exactly
      *> as the LINE spelling the general rules describe:
      *> GR5 a) first LINE clause absolute: first line number =
      *>   integer-1.
      *> GR5 b) 3. relative first clause of a body group that is not the
      *>   first
      *>   on the page: LINE-COUNTER + integer-2.
      *> GR7 a)/b) subsequent clause: absolute -> integer-1; relative ->
      *>   LINE-COUNTER + integer-2.
      *> GR9 "A multiple LINE clause is functionally equivalent to a
      *>   LINE
      *>   clause with a single operand, together with a simple OCCURS
      *>     clause
      *>   whose integer is equal to the number of operands"
      *>   cite.py: OK  §13.18.35.4 9)  (General rules)
      *> GR7 "Any unoccupied lines on the page result in a blank line."
      *>   cite.py: OK  §13.18.35.4 7) b)  (General rules)
      *> REPORT R: PAGE LIMIT 12, HEADING 1, FIRST DETAIL 3, LAST DETAIL
      *>   10.
      *> DERIVATION (LC = LINE-COUNTER; INITIATE sets LC 0):
      *>  GENERATE DE-A (first body group: no page fit test):
      *>    "LINES 3."          absolute -> line 3 "A"        LC 3
      *>    "LINES ARE PLUS 2." relative -> 3 + 2 = line 5 "a" LC 5
      *>  GENERATE DE-M: first clause absolute 7 > LC 5 -> fit succeeds
      *>    (GR4 b)); "LINES ARE 7 9" = two lines (GR9): line 7 "M",
      *>    line 9 "M"; LC 9
      *>  GENERATE DE-R: "LINES + 1" relative, fit 9 + 1 = 10 <= LAST
      *>    DETAIL
      *>    10 -> line 10 "R"; LC 10
      *>  Lines 1, 2, 4, 6, 8 are unoccupied -> blank.
      *> The read-back numbers each physical line of the page (a form
      *>   feed only
      *> restarts the numbering and shows nothing) and prints its first
      *>   2
      *> characters.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C16R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "L1C16R.TXT".
           SELECT CHK ASSIGN TO "L1C16R.TXT".
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
       RD  R PAGE LIMIT 12 LINES HEADING 1 FIRST DETAIL 3
              LAST DETAIL 10.
       01  DE-A TYPE DE.
           02  LINES 3.
               03  COLUMN 1 PIC X VALUE "A".
           02  LINES ARE PLUS 2.
               03  COLUMN 1 PIC X VALUE "a".
       01  DE-M TYPE DE.
           02  LINES ARE 7 9.
               03  COLUMN 1 PIC X VALUE "M".
       01  DE-R TYPE DE.
           02  LINES + 1.
               03  COLUMN 1 PIC X VALUE "R".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           GENERATE DE-A.
           GENERATE DE-M.
           GENERATE DE-R.
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
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:2) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
