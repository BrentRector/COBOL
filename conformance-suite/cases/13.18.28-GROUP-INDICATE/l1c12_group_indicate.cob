      *>--- conformance-case
      *> id: l1c12_group_indicate
      *> rules: [GR-13.18.28.4-1, FMT-13.18.28.2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.28.2 — GROUP INDICATE / GROUP (INDICATE optional):
      *>   printed only on the first detail
      *> General format: "GROUP INDICATE" with only GROUP underlined, so
      *>   INDICATE is an optional word.
      *>   cite.py --check 13.18.28.2 "GROUP"  -> OK  §13.18.28.2
      *>     (General format)
      *> SR1: "The GROUP INDICATE clause may be specified only within a
      *>   detail report group description, in
      *> an elementary entry that also contains a COLUMN clause and a
      *>   SOURCE or VALUE clause."
      *>   cite.py --check 13.18.28.3 "The GROUP INDICATE clause may be
      *>     specified only within a detail
      *>     report group description, in an elementary entry that also
      *>       contains a COLUMN clause and a
      *>     SOURCE or VALUE clause."  -> OK  §13.18.28.3 1)  (Syntax
      *>       rule)
      *> GR1: "The GROUP INDICATE clause has the same effect as a
      *>   PRESENT WHEN clause where the associated
      *> condition is true only on the first occasion that a GENERATE is
      *>   issued for the current detail
      *> group after any of the following events."  a) an INITIATE, b) a
      *>   page advance, c) a control break.
      *>   cite.py --check 13.18.28.4 "The GROUP INDICATE clause has the
      *>     same effect as a PRESENT WHEN
      *>     clause where the associated condition is true only on the
      *>       first occasion that a GENERATE is
      *>     issued for the current detail group after any of the
      *>       following events."
      *>     -> OK  §13.18.28.4 1)  (General rule)
      *> Both spellings are written: COLUMN 1 (SOURCE K1) says GROUP
      *>   INDICATE, COLUMN 4 (VALUE "HDR") says
      *> GROUP alone.  COLUMN 8 (SOURCE V) has no clause and prints on
      *>   every detail.  The report has no
      *> PAGE clause, so there is no page advance (event b) cannot
      *>   occur).  The report file is read back
      *> byte by byte and each non-empty line is DISPLAYed between
      *>   brackets.
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   INITIATE; K1=AA V=1 GENERATE : first detail after INITIATE
      *>     (a)  => [AA HDR 1]
      *>   K1=AA V=2 GENERATE           : no event, indicated items
      *>     absent => [       2]
      *>   K1=BB V=3 GENERATE           : control break on K1 (c)
      *>     => [BB HDR 3]
      *>   K1=BB V=4 GENERATE           : no event
      *>     => [       4]
      *>   TERMINATE.  (No CH/CF groups are defined, so nothing else
      *>     prints.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C12I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "L1C12I.RPT".
           SELECT CHK ASSIGN TO "L1C12I.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-G.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  K1      PIC XX    VALUE "AA".
       01  V       PIC 9     VALUE 1.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-G CONTROL IS K1.
       01  DE1 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC XX SOURCE K1 GROUP INDICATE.
           02  COLUMN 4 PIC X(3) VALUE "HDR" GROUP.
           02  COLUMN 8 PIC 9 SOURCE V.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-G.
           GENERATE DE1.
           MOVE 2 TO V.
           GENERATE DE1.
           MOVE "BB" TO K1.
           MOVE 3 TO V.
           GENERATE DE1.
           MOVE 4 TO V.
           GENERATE DE1.
           TERMINATE R-G.
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
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D" AND CHK-REC NOT = X"0C"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
