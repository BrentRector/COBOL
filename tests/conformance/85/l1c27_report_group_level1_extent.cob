      *> ISO §13.15.4 1) — a level 1 entry and its subordinates = a
      *> group
      *> "Each level 1 entry identifies a report group. The report group
      *> is defined by this entry and all its subordinate entries."
      *> cite.py --check 13.15.4 "Each level 1 entry identifies a report
      *>   group. The report group is defined by this entry and all its
      *>   subordinate entries" -> OK §13.15.4 1)
      *> Supporting rules (each --check OK):
      *>   §13.18.39.4 2) a) "If integer-1 is not specified, the report
      *>     consists of a single page of indefinite length" (RD R-1 has
      *>     no PAGE clause)
      *>   §13.18.35.4 5) c) "If the first LINE NUMBER clause of the
      *>     report group is relative and the report is not divided into
      *>     pages, the report group's first line number is obtained by
      *>     adding integer-2 to the current value of the report's
      *>     LINE-COUNTER" (cite.py labels it 5) 5. c))
      *>   §13.18.35.4 7) b) "If the LINE clause is relative, the new
      *>     line number is determined by adding integer-2 to the
      *>     report's LINE-COUNTER"
      *> The first group is written with level-number 1, the second with
      *> 01: both are level 1 entries. D1 owns ONE line holding A and B;
      *> D2 owns TWO lines, C then D. Each GENERATE prints exactly the
      *> lines of its own group's subordinate entries and nothing of the
      *> other's.
      *>
      *> DERIVATION (LINE-COUNTER starts at 0, every LINE is PLUS 1):
      *>   GENERATE D2 -> line 1 "C", line 2 " D"
      *>   GENERATE D1 -> line 3 "A B"
      *>   GENERATE D2 -> line 4 "C", line 5 " D"
      *> The read-back DISPLAYs each physical line after a "|" (trailing
      *> spaces are not significant): |C, | D, |A B, |C, | D.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C27D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "L1C27D.TXT".
           SELECT CHK ASSIGN TO "L1C27D.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-1.
       1   D1 TYPE DE.
           3   LINE PLUS 1.
               5   COLUMN 1 PIC X VALUE "A".
               5   COLUMN 3 PIC X VALUE "B".
       01  D2 TYPE DE.
           3   LINE PLUS 1.
               5   COLUMN 1 PIC X VALUE "C".
           3   LINE PLUS 1.
               5   COLUMN 2 PIC X VALUE "D".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE D2.
           GENERATE D1.
           GENERATE D2.
           TERMINATE R-1.
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
           DISPLAY "|" WS-LINE.
           MOVE 0 TO WS-I.
           MOVE SPACES TO WS-LINE.
