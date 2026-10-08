      *> kb/Work PB244 shape (b), the CELL-BACKED half - a variable-length group
      *> that holds a dynamic-length item AND an OCCURS DEPENDING table, when the
      *> group lives in a shared storage area (an EXTERNAL record) rather than in
      *> the program's own record struct.
      *>
      *> 14.9.11.4 GR7: "If identifier-1 references a variable-length group, the
      *> format in which its contents are displayed is defined by the implementor"
      *> - docs/CONFORMANCE.md DOC-A.1-57: the members' images in declaration
      *> order, the dynamic-length item at its CURRENT content and the table at its
      *> CURRENT count (13.18.38.4 GR8). 13.18.38.3 SR22 makes the table the
      *> trailing storage of its record, so the current image is the prefix of the
      *> maximum one. The EXTERNAL group must show the SAME characters as the same
      *> description declared in the program (G2 below), for every operation that
      *> takes the group whole: DISPLAY, FUNCTION LENGTH (15.50.4 rule 7),
      *> MOVE as sender and receiver (14.9.25.4 GR9) and comparison (8.8.4.2.17).
      *>
      *> EXPECTED VALUES, DERIVED (H "h", D "abc", T "x" "y" "z"):
      *>   A1 K=2: h + abc + x + y                         = habcxy    6
      *>   A2 K=3: the third occurrence joins              = habcxyz   7
      *>   A3 K=1: only the first occurrence is used       = habcx     5
      *>   B1 MOVE G TO G2 at K=1, then K=3: K is outside G2 and holds 1 at the
      *>      start of the operation, so only the first occurrence of G2's table is
      *>      used (13.18.38.4 GR8 a); the others were never written and are blank
      *>                                                   = habcx<2 spaces>
      *>   B2 G2 set to Q mm 1 2 3, K=2, MOVE G2 TO G, K=3: K is outside G and
      *>      holds 2, so the sender's two occurrences land and the third lies
      *>      beyond the part of G's table the operation uses (GR8 a) - it keeps
      *>      the "z" it held                              = Qmm12z
      *>   B3 K=3: G (Qmm12z) is not G2 (Qmm123)          = NE
      *>   B4 K=2: both groups use their first two occurrences = EQ
      *>   C  a Format-2 CALL passes the group BY REFERENCE at its MAXIMUM length
      *>      (14.8.2.2) and the formal occupies the argument's storage (14.2.3
      *>      GR8): the callee shows hZZx98 after its stores reached the caller;
      *>      BY CONTENT it works on a copy, so the caller shows the same.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244CEL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 EXTERNAL.
       01 G EXTERNAL.
          05 H PIC X.
          05 D PIC X DYNAMIC LENGTH LIMIT 5.
          05 T PIC X OCCURS 1 TO 3 DEPENDING ON K.
       01 G2.
          05 H2 PIC X.
          05 D2 PIC X DYNAMIC LENGTH LIMIT 5.
          05 T2 PIC X OCCURS 1 TO 3 DEPENDING ON K.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 2 TO K
           MOVE "h" TO H
           MOVE "abc" TO D
           MOVE "x" TO T(1)
           MOVE "y" TO T(2)
           MOVE "z" TO T(3)
           DISPLAY "A1=[" G "] " FUNCTION LENGTH(G)
           MOVE 3 TO K
           DISPLAY "A2=[" G "] " FUNCTION LENGTH(G)
           MOVE 1 TO K
           DISPLAY "A3=[" G "] " FUNCTION LENGTH(G)
           MOVE G TO G2
           MOVE 3 TO K
           DISPLAY "B1=[" G2 "]"
           MOVE "Q" TO H2
           MOVE "mm" TO D2
           MOVE "1" TO T2(1)
           MOVE "2" TO T2(2)
           MOVE "3" TO T2(3)
           MOVE 2 TO K
           MOVE G2 TO G
           MOVE 3 TO K
           DISPLAY "B2=[" G "]"
           IF G = G2 DISPLAY "B3 EQ" ELSE DISPLAY "B3 NE" END-IF
           MOVE 2 TO K
           IF G = G2 DISPLAY "B4 EQ" ELSE DISPLAY "B4 NE" END-IF
           MOVE "h" TO H
           MOVE "abc" TO D
           MOVE "x" TO T(1)
           MOVE "y" TO T(2)
           MOVE "z" TO T(3)
           CALL "PB244CSA" AS NESTED USING G K
           DISPLAY "C1=[" G "] " K
           CALL "PB244CSA" AS NESTED USING BY CONTENT G BY CONTENT K
           DISPLAY "C2=[" G "] " K
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244CSA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK PIC 9.
       01 LG.
          05 LH PIC X.
          05 LD PIC X DYNAMIC LENGTH LIMIT 5.
          05 LT PIC X OCCURS 1 TO 3 DEPENDING ON LK.
       PROCEDURE DIVISION USING LG LK.
       SA.
           DISPLAY "S=[" LG "] " FUNCTION LENGTH(LG)
           MOVE "ZZ" TO LD
           MOVE "9" TO LT(2)
           MOVE 3 TO LK
           MOVE "8" TO LT(3)
           GOBACK.
       END PROGRAM PB244CSA.
       END PROGRAM PB244CEL.
