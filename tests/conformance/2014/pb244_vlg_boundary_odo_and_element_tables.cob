      *> kb/Work PB244 shape (b) - the two variable-length mechanisms of
      *> PB244VLB / PB244REC ACROSS A FORMAT-2 CALL BOUNDARY: a group that holds a
      *> dynamic-length item AND an OCCURS DEPENDING table, and one whose
      *> dynamic-length items sit inside a fixed OCCURS table.
      *>
      *> 14.9.4.3 SR25 applies 14.8.2 to a Format-2 CALL, and 14.8.2.2 requires an
      *> argument and formal that are variable-length groups to be COMPATIBLE as
      *> 8.5.1.12 describes: dynamic-length items correspond when they "start at
      *> the same relative byte positions within their groups" (8.5.1.12.2), each
      *> OCCURRENCE of a table element being its own dynamic-length item, and
      *> "all dynamic-length elementary items are considered to be of zero length"
      *> (8.5.1.12.3). For an occurs-depending group passed BY REFERENCE "the
      *> maximum length is used" (14.8.2.2), and 14.2.3 GR8 makes the formal occupy
      *> the argument's storage, so the callee's stores - including to the
      *> DEPENDING item, here passed as a second argument - reach the caller.
      *> BY CONTENT (14.2.3 GR9) operates on a copy, so the caller sees nothing.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   S1 the callee sees H "H", D "abc", T(1..2) "xy" at K=2   -> Habcxy
      *>   A  after the callee sets D "QQ", T(2) "9", LK 3, T(3) "8": caller K=3
      *>      H "H" D "QQ" T "x" "9" "8"                             -> HQQx98 3
      *>   C  BY CONTENT: the callee sees the group as A left it (HQQx98, its
      *>      count 3 travelling with the content) and its stores reach a copy,
      *>      so the caller still shows                               -> HQQx98 3
      *>   S2 the callee sees H "H" (ab,1) (c,2)                     -> Hab1c2
      *>   B  after the callee sets DD(2) "QQQQ" and FX(1) "9"        -> Hab9QQQQ2
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244VLC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 VALUE 2.
       01 G1.
          05 H PIC X VALUE "H".
          05 D PIC X DYNAMIC LENGTH LIMIT 5.
          05 T PIC X OCCURS 1 TO 3 DEPENDING ON K.
       01 G2.
          05 H PIC X VALUE "H".
          05 TE OCCURS 2.
             10 DD PIC X DYNAMIC LENGTH LIMIT 5.
             10 FX PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "abc" TO D OF G1
           MOVE "x" TO T OF G1(1)
           MOVE "y" TO T OF G1(2)
           CALL "PB244SA" AS NESTED USING G1 K
           DISPLAY "A=[" G1 "] " K
           CALL "PB244SA" AS NESTED USING BY CONTENT G1 BY CONTENT K
           DISPLAY "C=[" G1 "] " K
           MOVE "ab" TO DD OF G2(1)
           MOVE "c" TO DD OF G2(2)
           MOVE "1" TO FX OF G2(1)
           MOVE "2" TO FX OF G2(2)
           CALL "PB244SB" AS NESTED USING G2
           DISPLAY "B=[" G2 "]"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244SA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK PIC 9.
       01 LG.
          05 H PIC X.
          05 D PIC X DYNAMIC LENGTH LIMIT 5.
          05 T PIC X OCCURS 1 TO 3 DEPENDING ON LK.
       PROCEDURE DIVISION USING LG LK.
       SA.
           DISPLAY "S1=[" LG "]"
           MOVE "QQ" TO D OF LG
           MOVE "9" TO T OF LG(2)
           MOVE 3 TO LK
           MOVE "8" TO T OF LG(3)
           GOBACK.
       END PROGRAM PB244SA.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244SB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG2.
          05 H PIC X.
          05 TE OCCURS 2.
             10 DD PIC X DYNAMIC LENGTH LIMIT 5.
             10 FX PIC X.
       PROCEDURE DIVISION USING LG2.
       SB.
           DISPLAY "S2=[" LG2 "]"
           MOVE "QQQQ" TO DD OF LG2(2)
           MOVE "9" TO FX OF LG2(1)
           GOBACK.
       END PROGRAM PB244SB.
       END PROGRAM PB244VLC.
