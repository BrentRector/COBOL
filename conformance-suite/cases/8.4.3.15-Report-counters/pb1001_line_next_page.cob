*>--- conformance-case
*> id: pb1001_line_next_page
*> rules: [GR-8.4.3.15.4-3, GR-13.18.35.4-4, GR-13.18.37.4-4, FMT-13.18.35.2, FMT-14.9.21.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> THE LINE CLAUSE'S NEXT PAGE PHRASE, BOTH OPERAND FORMS, ON A BODY GROUP AND ON A REPORT FOOTING
      *> (kb/Work PB1001 — `LINE integer-1 ON NEXT PAGE` was refused, COBOLNET0899, at every edition).
      *>
      *> THE RULES (ISO/IEC 1989:2023).
      *> §13.18.35.2 Format 1 — the operand is `integer-1 [ON NEXT PAGE]`, `{PLUS|+} integer-2` or a bare
      *>   `ON NEXT PAGE` (the three-way brace, rendered from the printed page).
      *> §13.18.35.4 GR4 — a body group other than "the chronologically first body group since the execution
      *>   of an INITIATE statement" takes a page fit test, and a) "If the first LINE clause has a NEXT PAGE
      *>   phrase, no page fit test takes place and the page fit is declared unsuccessful." "Which LINE clause is
      *>   taken to be the first may depend on the current values of conditions in PRESENT WHEN clauses."
      *> §13.18.35.4 GR5 a) — an absolute first line is printed at integer-1; "If in addition the NEXT PAGE phrase
      *>   is present and the report group is a report footing, the first line is printed beginning on a new
      *>   page." GR5 b) 3. — the first body group on a page begins at the FIRST DETAIL integer.
      *> §13.18.37.4 GR4 a) 2. — after an absolute NEXT GROUP filled the save location, a next body group that
      *>   begins "with an absolute LINE clause with the NEXT PAGE phrase" takes a page advance, the save
      *>   location moves to LINE-COUNTER, and it is then processed "as for an identical report group without
      *>   the NEXT PAGE phrase". ONE advance: the one the save location forces (docs/CONFORMANCE.md).
      *> §13.18.57.4 GR6 b) — no page heading before "a report footing on a page by itself"; GR6 f) 2. — no page
      *>   footing "on the last page, if it is occupied only by a report footing group"; GR7 f) — the upper
      *>   limit of such a footing is the HEADING integer (where the bare form, which writes no integer, begins
      *>   — a determination, docs/CONFORMANCE.md).
      *> §8.4.3.15.4 GR2/GR3 — each page advance adds 1 to PAGE-COUNTER and zeroes LINE-COUNTER.
      *>
      *> REPORT R-NP: PAGE LIMIT 12, HEADING 1, FIRST DETAIL 3, LAST DETAIL 9, FOOTING 10 (§13.18.39).
      *> DERIVATION (LC = LINE-COUNTER, PC = PAGE-COUNTER; INITIATE: LC 0, PC 1):
      *>  1 GENERATE DE-N (the first GENERATE): PH line 1 "PH1". DE-N is the chronologically first body group
      *>    — no page fit test, so NO advance despite NEXT PAGE (GR4): line 5 "N05", line 6 "n06". LC 6.
      *>  2 GENERATE DE-A: fit 6 + 1 = 7 <= 9 -> line 7 "A07".
      *>  3 GENERATE DE-N: NEXT PAGE -> fit unsuccessful (GR4 a)) -> page advance: PF at FOOTING + 1 = 11
      *>    "PF1"; PC 2; PH "PH2"; line 5 "N05", line 6 "n06".
      *>  4 GENERATE DE-B (bare NEXT PAGE): advance: PF "PF2"; PC 3; PH "PH3"; the first body group on the page
      *>    -> FIRST DETAIL 3 (GR5 b) 3.) "B03", then "b04". LC 4.
      *>  5 WS-FLAG "N", GENERATE DE-P: its NEXT PAGE line is ABSENT, so the relative line is the first: trial
      *>    4 + 1 = 5 <= 9 -> fits, line 5 "p05" on THIS page.
      *>  6 WS-FLAG "Y", GENERATE DE-P: the NEXT PAGE line is present and first -> advance: PF "PF3"; PC 4;
      *>    PH "PH4"; line 6 "P06", line 7 "p07".
      *>  7 GENERATE DE-G: fit 7 + 1 = 8 -> line 8 "G08". NEXT GROUP 4: LC 8 is not below 4, so 4 goes to the
      *>    save location and LC = FOOTING 10.
      *>  8 GENERATE DE-N: the save location's advance: PF "PF4"; PC 5; PH "PH5". GR4 a) 2.: LC = 4, and the
      *>    phrase-less fit test 5 > 4 succeeds -> line 5 "N05", line 6 "n06" on page 5 (no second advance).
      *>  9 TERMINATE: PF "PF5" on page 5; the RF begins a new page: PC 6, no PH; line 4 "RF6"; no PF after it.
      *>
      *> REPORT R-BF: PAGE LIMIT 6, HEADING 2, FIRST DETAIL 3. DE-X: first body group -> FIRST DETAIL 3 "X03".
      *>  TERMINATE: the RF's bare NEXT PAGE -> new page, PC 2, first line at HEADING 2 "RF2", then "r03".
      *>
      *> THE READ-BACK numbers every physical line of each page (the PB484 byte reader) and shows each form
      *> feed as a PAGE marker.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1001NP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1001np.txt".
           SELECT PR2 ASSIGN TO "pb1001np2.txt".
           SELECT CHK ASSIGN TO "pb1001np.txt".
           SELECT CH2 ASSIGN TO "pb1001np2.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-NP.
       FD  PR2 REPORT IS R-BF.
       FD  CHK.
       01  CHK-REC PIC X.
       FD  CH2.
       01  CH2-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-FLAG PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-NP PAGE LIMIT 12 LINES HEADING 1 FIRST DETAIL 3
                LAST DETAIL 9 FOOTING 10.
       01  PH-G TYPE PH.
           02  LINE 1.
               03  COLUMN 1 PIC XX VALUE "PH".
               03  COLUMN 3 PIC 9 SOURCE PAGE-COUNTER.
       01  DE-A TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "A".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
       01  DE-N TYPE DE.
           02  LINE 5 ON NEXT PAGE.
               03  COLUMN 1 PIC X VALUE "N".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "n".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
       01  DE-B TYPE DE.
           02  LINE NEXT PAGE.
               03  COLUMN 1 PIC X VALUE "B".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "b".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
       01  DE-P TYPE DE.
           02  LINE NUMBER IS 6 ON NEXT PAGE PRESENT WHEN WS-FLAG = "Y".
               03  COLUMN 1 PIC X VALUE "P".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "p".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
       01  DE-G TYPE DE NEXT GROUP 4.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "G".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
       01  PF-G TYPE PF.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC XX VALUE "PF".
               03  COLUMN 3 PIC 9 SOURCE PAGE-COUNTER.
       01  RF-G TYPE RF.
           02  LINE 4 ON NEXT PAGE.
               03  COLUMN 1 PIC XX VALUE "RF".
               03  COLUMN 3 PIC 9 SOURCE PAGE-COUNTER.
       RD  R-BF PAGE LIMIT 6 LINES HEADING 2 FIRST DETAIL 3.
       01  DE-X TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "X".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
       01  RF-B TYPE RF.
           02  LINE ON NEXT PAGE.
               03  COLUMN 1 PIC XX VALUE "RF".
               03  COLUMN 3 PIC 9 SOURCE PAGE-COUNTER.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "r".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT PR2.
           INITIATE R-NP R-BF.
           GENERATE DE-N.
           GENERATE DE-A.
           GENERATE DE-N.
           GENERATE DE-B.
           GENERATE DE-P.
           MOVE "Y" TO WS-FLAG.
           GENERATE DE-P.
           GENERATE DE-G.
           GENERATE DE-N.
           GENERATE DE-X.
           TERMINATE R-NP R-BF.
           CLOSE PRT PR2.
           DISPLAY "==== R-NP ====".
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
           DISPLAY "==== R-BF ====".
           MOVE "N" TO WS-EOF.
           MOVE 0 TO WS-LN.
           OPEN INPUT CH2.
           PERFORM UNTIL WS-EOF = "Y"
               READ CH2
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CH2-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CH2.
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
                   DISPLAY "---- PAGE ----"
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:4) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
