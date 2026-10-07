      *> THE NEXT GROUP CLAUSE, EVERY FORM A BODY GROUP, A REPORT HEADING AND A PAGE FOOTING CAN WRITE
      *> (kb/Work PB957 — the clause was refused by name, COBOLNET0899, at every edition; kb/Work PB951 —
      *> `+` was a bare parse error where the standard makes it the word PLUS's synonym).
      *>
      *> THE RULES.
      *> §13.18.37.3 SR2 / §13.18.35.3 SR1 / §13.18.14.3 SR2 — "PLUS and + are synonyms." DE-A writes
      *>   NEXT GROUP PLUS 2 and DE-B writes NEXT GROUP + 2 (and LINE + 1 beside LINE PLUS 1); each pair
      *>   must place identically. (The relative COLUMN form is a 2002 addition, so the COLUMN spelling pair
      *>   is pinned by pb951_relative_sign_synonyms at 2002.)
      *> §13.18.37.4 GR2 — the clause "modifies the value of the current report's LINE-COUNTER after the
      *>   printing of the last line" of its group.
      *> GR3 b) (report heading, relative) — "integer-2 is added to LINE-COUNTER".
      *> GR4 a) (body group, absolute) — if LINE-COUNTER is less than integer-1 it is set to integer-1;
      *>   otherwise integer-1 goes to a save location, LINE-COUNTER is set to the FOOTING integer, a page
      *>   advance takes place before the next non-dummy body group, and GR4 a) 3. places a next group of
      *>   relative lines "on the next line following the line number in the save location".
      *> GR4 b) (body, relative) — integer-2 is added when the sum is less than FOOTING.
      *> GR4 c) (body, NEXT PAGE) — "the FOOTING integer is moved to LINE-COUNTER".
      *> GR5 b) (page footing, relative) — integer-2 is added to LINE-COUNTER, which moves a report footing
      *>   placed relative to it (§13.18.35.4 GR5 b) 5.: LINE-COUNTER + integer-2 after a page footing).
      *> GR6 — WITH RESET: PAGE-COUNTER is set to 1 at the next page advance (§14.9.16.4 GR6 d)).
      *>
      *> THE PAGE: PAGE LIMIT 20, HEADING 1, FIRST DETAIL 5, LAST DETAIL 14, FOOTING 16 (§13.18.39).
      *> DERIVATION, group by group (LC = LINE-COUNTER, PC = PAGE-COUNTER; INITIATE: LC 0, PC 1).
      *>  GENERATE DE-A (the first GENERATE, §14.9.16.4 GR4):
      *>   RH    LINE 1 absolute -> line 1 "RH". GR3 b): LC 1 + 1 = 2.
      *>   PH    relative, a report heading on this page -> LC + 1 = 3 (§13.18.35.4 GR5 b) 2.): "PH 1".
      *>         (Without the RH's NEXT GROUP it would print on line 2.)
      *>   DE-A  first body group on the page -> FIRST DETAIL 5 (GR5 b) 3.): "A05". GR4 b): 5 + 2 = 7.
      *>  GENERATE DE-B: page fit 7 + 1 = 8 <= 14 -> line 8 "B08". GR4 b) (+): 8 + 2 = 10.
      *>  GENERATE DE-C: 10 + 1 = 11 -> line 11 "C11". GR4 a): 11 < 12, so LC = 12.
      *>  GENERATE DE-A: 12 + 1 = 13 -> line 13 "A13". GR4 b): 13 + 2 = 15 < 16, so LC = 15.
      *>  GENERATE DE-D: page fit 15 + 1 = 16 > 14 FAILS -> page advance (§14.9.16.4 GR6):
      *>   PF    FOOTING + 1 = 17 "PF 1"; GR5 b): LC 17 + 1 = 18. Form feed, PC 2, LC 0.
      *>   PH    no report heading on this page -> HEADING + 1 - 1 = 1: "PH 2".
      *>   DE-D  FIRST DETAIL 5 "D05". GR4 c): LC = FOOTING 16; GR6: the next advance resets PC.
      *>  GENERATE DE-A: 16 + 1 = 17 > 14 -> advance: PF line 17 "PF 2"; form feed; PC = 1 (GR6, NOT 3);
      *>   PH line 1 "PH 1"; DE-A at FIRST DETAIL 5 "A05"; LC 5 + 2 = 7.
      *>  GENERATE DE-E: 7 + 1 = 8 -> line 8 "E08". GR4 a): LC 8 is NOT less than 6, so 6 goes to the
      *>   save location and LC = FOOTING 16.
      *>  GENERATE DE-B: GR4 a) — the page advance: PF line 17 "PF 1"; form feed; PC 2; PH line 1 "PH 2".
      *>   DE-B has only relative lines, so GR4 a) 3.: its first line is 6 + 1 = 7 (NOT FIRST DETAIL 5):
      *>   "B07". GR4 b): LC 9.
      *>  TERMINATE (§14.9.46.4 GR3; no CONTROL clause): PF line 17 "PF 2"; §13.18.37.4 GR5 b) LC 18; RF, a page
      *>   footing printed on this page -> LC + 1 = 19 "RF" (without the PF's NEXT GROUP, line 18).
      *>
      *> THE READ-BACK numbers every physical line of each page (the PB484 byte reader) and shows each form
      *> feed as a PAGE marker, so every blank line the clause produced is visible.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB957NGF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb957ngf.txt".
           SELECT CHK ASSIGN TO "pb957ngf.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-NG.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-NG PAGE LIMIT 20 LINES HEADING 1 FIRST DETAIL 5
                LAST DETAIL 14 FOOTING 16.
       01  RH-G TYPE RH NEXT GROUP PLUS 1.
           02  LINE 1.
               03  COLUMN 1 PIC X(2) VALUE "RH".
       01  PH-G TYPE PH.
           02  LINE + 1.
               03  COLUMN 1 PIC X(2) VALUE "PH".
               03  COLUMN 4 PIC 9 SOURCE PAGE-COUNTER.
       01  DE-A TYPE DE NEXT GROUP PLUS 2.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "A".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
       01  DE-B TYPE DE NEXT GROUP + 2.
           02  LINE + 1.
               03  COLUMN 1 PIC X VALUE "B".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
       01  DE-C TYPE DE NEXT GROUP IS 12.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "C".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
       01  DE-D TYPE DE NEXT GROUP NEXT PAGE WITH RESET.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "D".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
       01  DE-E TYPE DE NEXT GROUP 6.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "E".
               03  COLUMN 2 PIC 99 SOURCE LINE-COUNTER.
       01  PF-G TYPE PF NEXT GROUP + 1.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "PF".
               03  COLUMN 4 PIC 9 SOURCE PAGE-COUNTER.
       01  RF-G TYPE RF.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) VALUE "RF".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-NG.
           GENERATE DE-A.
           GENERATE DE-B.
           GENERATE DE-C.
           GENERATE DE-A.
           GENERATE DE-D.
           GENERATE DE-A.
           GENERATE DE-E.
           GENERATE DE-B.
           TERMINATE R-NG.
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
