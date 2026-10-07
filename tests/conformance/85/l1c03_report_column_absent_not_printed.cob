      *> ISO §13.18.14.4 GR1 and GR11 - an entry with no COLUMN clause
      *> is not printed; a LINE with no printable item is a blank line
      *>
      *> "1) The COLUMN clause defines one or more printable items. If a
      *> COLUMN clause is not specified, the items are not printed."
      *>   cite.py: OK  §13.18.14.4 1)  (General rules)
      *> "11) If an entry containing a LINE clause has no subordinate
      *> entry defining a printable item, the resultant report line
      *> will be blank."
      *>   cite.py: OK  §13.18.14.4 11)  (General rules)
      *> Placement (RD with no PAGE clause = one page of indefinite
      *> length, every LINE clause relative):
      *>   "If integer-1 is not specified, the report consists of a
      *>   single page of indefinite length"
      *>   cite.py: OK  §13.18.39.4 2) a)  (General rules)
      *>   "If the report is not divided into pages, all its LINE
      *>   clauses shall be relative."
      *>   cite.py: OK  §13.18.35.3 5)  (Syntax rules)
      *>   "If the first LINE NUMBER clause of the report group is
      *>   relative and the report is not divided into pages, the report
      *>   group's first line number is obtained by adding integer-2 to
      *>   the current value of the report's LINE-COUNTER"
      *>   cite.py: OK  §13.18.35.4 5) 5. c)  (General rules)
      *>   subsequent relative LINE: "the new line number is determined
      *>   by adding integer-2 to the report's LINE-COUNTER"
      *>   cite.py: OK  §13.18.35.4 7) b)  (General rules)
      *>   "Any unoccupied columns in each print line are filled with
      *>   space characters."
      *>   cite.py: OK  §13.18.14.4 10)  (General rules)
      *>
      *> DERIVATION. LINE-COUNTER starts at 0 (INITIATE); DET is one
      *> report group of four relative lines, so lines 1, 2, 3, 4.
      *>   line 1: COLUMN 1 "ONE", an entry SOURCE WS-HID with NO COLUMN
      *>           clause (GR1: not printed), COLUMN 5 "TWO"
      *>           -> "ONE TWO" (column 4 unoccupied -> space, GR10).
      *>           If GR1 were broken, "HIDE" would appear in the line.
      *>   line 2: a LINE entry with NO subordinate entry -> blank
      *>   (GR11).
      *>   line 3: a LINE entry whose only subordinate has no COLUMN
      *>           clause, so it defines no printable item -> blank
      *>           (GR11 with GR1).
      *>   line 4: COLUMN 1 "THREE".
      *> The read-back numbers every physical line, blanks included
      *> (a second SELECT over the file with a one-character record,
      *> the reader the 85 golden pb484_report_line_placement uses).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C03H.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "l1c03h.txt".
           SELECT CHK ASSIGN TO "l1c03h.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-H.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-HID  PIC X(4)  VALUE "HIDE".
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-H.
       01  DET TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(3) VALUE "ONE".
               03  PIC X(4) SOURCE WS-HID.
               03  COLUMN 5 PIC X(3) VALUE "TWO".
           02  LINE PLUS 1.
           02  LINE PLUS 1.
               03  PIC X(4) SOURCE WS-HID.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(5) VALUE "THREE".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-H.
           GENERATE DET.
           TERMINATE R-H.
           CLOSE PRT.
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
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:8) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
