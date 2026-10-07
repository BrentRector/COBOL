      *> THE NEXT GROUP CLAUSE AT A CONTROL BREAK, ON A REPORT HEADING ALONE ON ITS PAGE, AND BEFORE A
      *> TERMINATE (kb/Work PB957 — the clause was refused by name, COBOLNET0899, at every edition).
      *>
      *> THE RULES.
      *> §13.18.37.4 GR1 — "The NEXT GROUP clause has no effect when it is specified in a control footing
      *>   that is at a level other than the highest level at which the control break is detected."
      *> GR3 c) — "If NEXT GROUP NEXT PAGE is specified, the report heading is printed on the first page of
      *>   the report as the only report group on that page and LINE-COUNTER is then set equal to zero";
      *>   §14.9.16.4 GR4 a) — "an advance is made to the next physical page, and PAGE-COUNTER is either
      *>   incremented by 1 or, if the report heading's NEXT GROUP clause has the WITH RESET phrase, set to 1".
      *> GR4 a) — an absolute NEXT GROUP whose integer-1 LINE-COUNTER has already reached goes to a save
      *>   location with LINE-COUNTER set to FOOTING, and "will have no effect at all if a TERMINATE is next
      *>   executed for the report".
      *> GR4 b) — relative: integer-2 is added while the sum stays below FOOTING.
      *> §14.9.46.4 GR3 b) — TERMINATE prints each control footing "as though a control break has been
      *>   sensed in the most major control data item", so only the MAJOR footing's clause applies there.
      *>
      *> THE PAGE: PAGE LIMIT 20, HEADING 1, FIRST DETAIL 3, LAST DETAIL 14, FOOTING 16; CONTROLS ARE
      *> WS-MAJ (level 0) WS-MIN (level 1). No page footing, so a page advance prints none.
      *> DERIVATION (LC = LINE-COUNTER, PC = PAGE-COUNTER; INITIATE: LC 0, PC 1).
      *>  GENERATE 1 (MAJ 1, MIN 1 — the first GENERATE):
      *>   RH    line 1 "RH"; GR3 c): the page ends there — form feed, PC set to 1 (WITH RESET), LC 0.
      *>   PH    line 1 "PH 1". DE first body group: FIRST DETAIL 3 "D03"; GR4 a) 3 < 10 so LC = 10.
      *>  GENERATE 2 (MAJ 1, MIN 2 — a break at level 1, which IS the highest level broken):
      *>   CF-N  fit 10 + 1 = 11 <= 16: "N11"; GR4 b) 11 + 3 = 14 < 16, LC 14.
      *>   DE    fit 14 + 1 = 15 > 14 FAILS: form feed, PC 2, PH "PH 2"; DE at FIRST DETAIL "D03"; LC 10.
      *>  GENERATE 3 (MAJ 2, MIN 1 — a break at level 0):
      *>   CF-N  is BELOW the break level: printed "N11" but GR1 — no effect, LC stays 11.
      *>   CF-J  the break level: "J12"; GR4 b) 12 + 2 = 14, LC 14. (Had CF-N's clause applied: "J15".)
      *>   DE    15 > 14: form feed, PC 3, "PH 3", "D03", LC 10.
      *>  GENERATE 4 (no break): DE fit 11 <= 14 -> "D11"; GR4 a): 11 is NOT less than 10, so 10 goes to
      *>   the save location and LC = FOOTING 16.
      *>  TERMINATE: GR4 a) — no effect at all: LC is 11 again. CF-N "N12" (below the most major level, no
      *>   effect); CF-J "J13". (Had the save location stood, CF-N's fit test 16 + 1 > 16 would have
      *>   advanced the page first.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB957NGC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb957ngc.txt".
           SELECT CHK ASSIGN TO "pb957ngc.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-CT.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-MAJ  PIC 9     VALUE 1.
       01  WS-MIN  PIC 9     VALUE 1.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-CT CONTROLS ARE WS-MAJ WS-MIN
                PAGE LIMIT 20 LINES HEADING 1 FIRST DETAIL 3
                LAST DETAIL 14 FOOTING 16.
       01  RH-G TYPE RH NEXT GROUP NEXT PAGE WITH RESET.
           02  LINE 1.
               03  COLUMN 1 PIC X(2) VALUE "RH".
       01  PH-G TYPE PH.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "PH".
               03  COLUMN 4 PIC 9 SOURCE PAGE-COUNTER.
       01  DE-G TYPE DE NEXT GROUP 10.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "D".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
       01  CF-N TYPE CF WS-MIN NEXT GROUP PLUS 3.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "N".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
       01  CF-J TYPE CF WS-MAJ NEXT GROUP PLUS 2.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "J".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-CT.
           GENERATE DE-G.
           MOVE 2 TO WS-MIN.
           GENERATE DE-G.
           MOVE 2 TO WS-MAJ.
           MOVE 1 TO WS-MIN.
           GENERATE DE-G.
           GENERATE DE-G.
           TERMINATE R-CT.
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
           EVALUATE TRUE
               WHEN CHK-REC = SYM-X0A
                   PERFORM SHOW-LINE
               WHEN CHK-REC = SYM-X0C
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   DISPLAY "---- PAGE ----"
                   MOVE 0 TO WS-LN
               WHEN CHK-REC = SYM-X0D
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:4) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
