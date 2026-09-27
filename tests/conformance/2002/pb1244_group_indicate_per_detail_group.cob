      *> GROUP INDICATE IS A PRESENT WHEN ON THE ITEM, ARMED PER DETAIL GROUP (kb/Work PB1244).
      *> ISO/IEC 1989:2023 §13.18.28.4 GR1: "The GROUP INDICATE clause has the same effect as a PRESENT
      *> WHEN clause where the associated condition is true only on the first occasion that a GENERATE is
      *> issued for the current detail group after any of the following events." a) an INITIATE, b) a
      *> page advance, c) a control break.
      *>   cite.py --check 13.18.28.4 "true only on the first occasion that a GENERATE is issued for the
      *>     current detail group"  -> OK  §13.18.28.4 1)  (General rule)
      *> §13.18.41.4 GR2b: a false condition declares the item absent, "and the effect on processing is as
      *> though the entry were omitted" -- so an indicated item that is not presented is NOT a span of
      *> spaces: it occupies no column and does not move the horizontal counter.
      *>   cite.py --check 13.18.41.4 "the corresponding data item is declared to be absent and the effect
      *>     on processing is as though the entry were omitted"  -> OK  §13.18.41.4 2) b)
      *> §13.18.14.4 GR7-GR9: the horizontal counter starts each line at zero; a relative item's leftmost
      *> column is "obtained by adding integer-2 to the current line's horizontal counter", except that
      *> "If integer-2 is specified for an item that is the first printable item in the current line, it
      *> specifies the leftmost column of that item"; "The rightmost column position of each printable item
      *> becomes the new value of the horizontal counter."
      *>   cite.py --check 13.18.14.4 "If integer-2 is specified for an item that is the first printable
      *>     item in the current line, it specifies the leftmost column of that item"  -> OK  8)
      *>   cite.py --check 13.18.14.4 "The rightmost column position of each printable item becomes the
      *>     new value of the horizontal counter"  -> OK  9)
      *> Page fit (§13.18.35.4 GR4c): every fit below has the same outcome whether the trial sum adds the
      *> first LINE clause's integer-2 or not, so the derivation does not lean on that reading. The first
      *> body group on a page lands on FIRST DETAIL (§13.18.35.4 GR5b3).
      *>
      *> WHY EACH LEG CAN FAIL. DA and DB are two detail groups; each has its own condition, so a GENERATE
      *> of one neither reads nor consumes the other's (a report-wide flag prints DB's first line without
      *> its indicated item). DA's second line has an item under the columns of the indicated item on its
      *> first line; it has no GROUP INDICATE clause and prints every time (a per-group column mask blanks
      *> it). DA's "DA" and DB's WN follow an indicated item with a relative COLUMN: when the indicated
      *> item is absent they move left (a blanked-in-place item leaves them where they were), and the
      *> indicated item of DB itself has a relative COLUMN (formerly refused as not implemented).
      *>
      *> DERIVATION (PAGE LIMIT 9, FIRST DETAIL 1, LAST DETAIL 9; the report file is read back byte by
      *> byte, each line DISPLAYed between brackets and a form feed shown as [PAGE]):
      *>   INITIATE                     : a) arms DA and DB
      *>   WN=1 GENERATE DA  line 1-2   : armed. AAAAA 1-5, hc 5, "DA" at 5+2=7  => [AAAAA DA]
      *>                                  line 2: WB at column 1               => [BBBBB]
      *>   WN=2 GENERATE DB  line 3     : armed (DA's GENERATE did not consume it). "DB" 1-2, hc 2, WB at
      *>                                  2+2=4 (4-8), hc 8, WN at 8+2=10      => [DB BBBBB 2]
      *>   WN=3 GENERATE DA  line 4-5   : not armed: AAAAA absent, "DA" is the line's first printable item,
      *>                                  so PLUS 2 is column 2                => [ DA]
      *>                                  line 2 still prints                  => [BBBBB]
      *>   WN=4 GENERATE DB  line 6     : not armed: WB absent, hc 2, WN at 4 => [DB 4]
      *>   K "1" -> "2"                 : c) the next GENERATE detects a control break, arming both
      *>   WN=5 GENERATE DB  line 7     : armed                                => [DB BBBBB 5]
      *>   WN=6 GENERATE DA  line 8-9   : armed (the break armed DA too)       => [AAAAA DA] [BBBBB]
      *>   WN=7 GENERATE DA             : LINE-COUNTER 9 + 2 lines > LAST DETAIL 9 -> page advance, b)
      *>                                  arms both; DA is re-armed although it just printed
      *>                                                                         => [PAGE]
      *>                     line 1-2   :                                      => [AAAAA DA] [BBBBB]
      *>   WN=8 GENERATE DB  line 3     : armed by the page advance            => [DB BBBBB 8]
      *>   WN=9 GENERATE DB  line 4     : not armed                            => [DB 9]
      *>   TERMINATE (no heading or footing groups, so nothing else prints).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1244GI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB1244GI.RPT".
           SELECT CHK ASSIGN TO "PB1244GI.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  K       PIC X     VALUE "1".
       01  WA      PIC X(5)  VALUE "AAAAA".
       01  WB      PIC X(5)  VALUE "BBBBB".
       01  WN      PIC 9     VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R CONTROL IS K
           PAGE LIMIT IS 9 LINES FIRST DETAIL 1 LAST DETAIL 9.
       01  DA TYPE DETAIL.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(5) SOURCE WA GROUP INDICATE.
               03  COLUMN PLUS 2 PIC X(2) VALUE "DA".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(5) SOURCE WB.
       01  DB TYPE DETAIL LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "DB".
           03  COLUMN PLUS 2 PIC X(5) SOURCE WB GROUP INDICATE.
           03  COLUMN PLUS 2 PIC 9 SOURCE WN.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R.
           MOVE 1 TO WN. GENERATE DA.
           MOVE 2 TO WN. GENERATE DB.
           MOVE 3 TO WN. GENERATE DA.
           MOVE 4 TO WN. GENERATE DB.
           MOVE "2" TO K.
           MOVE 5 TO WN. GENERATE DB.
           MOVE 6 TO WN. GENERATE DA.
           MOVE 7 TO WN. GENERATE DA.
           MOVE 8 TO WN. GENERATE DB.
           MOVE 9 TO WN. GENERATE DB.
           TERMINATE R.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN CHK-REC = X"0A"
                   PERFORM SHOW-LINE
               WHEN CHK-REC = X"0C"
                   PERFORM SHOW-LINE
                   DISPLAY "[PAGE]"
               WHEN CHK-REC = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
