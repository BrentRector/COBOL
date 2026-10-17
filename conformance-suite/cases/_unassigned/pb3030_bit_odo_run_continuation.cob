      *>--- conformance-case
      *> id: pb3030_bit_odo_run_continuation
      *> rules: []
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3030 sibling (wave-1074 branch review R1-1): an
      *> OCCURS DEPENDING table of USAGE BIT items that CONTINUES a
      *> bit run - a DIRECT member of a variable-length group, after a
      *> bit item of the same level - as its current extent, its MOVE,
      *> a cell-backed group's image, an activation boundary and a
      *> file record.
      *>
      *> 8.5.1.6.3: "an elementary bit data item immediately following
      *>   an elementary bit data item or bit group item of the same
      *>   level" is placed "at the next bit position in storage", and
      *>   so is "a bit group item immediately following a bit group
      *>   item or elementary bit data item of the same level": B and
      *>   the occurrences of TB share ONE run of characters, and the
      *>   table's first occurrence starts inside B's character.
      *> 13.18.38.4 GR8 a): data-name-1 (N, N2) is outside the group,
      *>   so "only that part of the table area that is specified by
      *>   the value of the data item referenced by data-name-1 at the
      *>   start of the operation will be used", sending or receiving.
      *>   QREC holds QK itself: GR8 b) sends that part and receives
      *>   at "the maximum length of the group".
      *> 15.50.4 rule 7 sums a variable-length group's parts; rule 9
      *>   rounds a part of a character up.
      *> Before the fix the run's characters were counted at the
      *>   table's maximum AND the table cut from an empty field: G's
      *>   current image threw (a negative length) or, at a count
      *>   whose cut was zero, carried a NUL character LENGTH did not.
      *>
      *> Expected values, derived by hand (bits listed B then TB(1..)):
      *>   L3  DD "ab" (2) and 1 + 3 bits, one character: 3.
      *>   LX  1 + 16 bits, three characters: 5.
      *>   D   B = 0, TB = 101 pack into 01010000, "P": [abP].
      *>   L4  G4's B4 is nine bits, so TB4 starts at bit 1 of B4's
      *>       second character: 2 + ceiling(12 / 8) = 4.
      *>   D4  010000010 then 101: 01000001 01010000, "AP": [efAP].
      *>   L5  G5's H5 is a bit GROUP continuing B5's run: 0, 1, 101
      *>       is 5 bits, one character 01101000, "h": 3, [cdh].
      *>   M   G2 holds 1 then sixteen 1s; MOVE G TO G2 with N = 3 and
      *>       N2 = 2 takes B and TB2(1..2) from G (0, 1, 0) and keeps
      *>       the rest: zz becomes ab, bits 0 10111111.
      *>   C   GC (ADDRESS OF taken, so CELL-BACKED) holds G's shape:
      *>       [abP] at N = 3, LENGTH 3.
      *>   IN  the nested program's formal L is G at its maximum
      *>       (14.8.2.2, "the maximum length is used"): M = 16, so
      *>       2 + 3 = 5, bits 0 1010000 for LB and LTB(1..7).
      *>   AFTER  BY REFERENCE: the callee's MOVEs are G's: xyz, B 1.
      *>   R   a record written at QK = 9 (QB 0, QT 1..9 = 100000101)
      *>       and at QK = 3 (QT 100) reads back the same; W9 is
      *>       1 + 3 + ceiling(10 / 8) = 6, W3 is 1 + 2 + 1 = 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3030RUN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT QF ASSIGN TO "pb3030run.seq" ORGANIZATION SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD QF.
       01 QREC.
          05 QK PIC 9.
          05 QD PIC X DYNAMIC LENGTH LIMIT 5.
          05 QB PIC 1 USAGE BIT.
          05 QT PIC 1 USAGE BIT OCCURS 1 TO 16 DEPENDING ON QK.
       WORKING-STORAGE SECTION.
       01 N PIC 99 VALUE 3.
       01 N2 PIC 99 VALUE 16.
       01 G.
          05 DD PIC X DYNAMIC LENGTH LIMIT 5.
          05 B PIC 1 USAGE BIT.
          05 TB PIC 1 USAGE BIT OCCURS 1 TO 16 DEPENDING ON N.
       01 G2.
          05 DD2 PIC X DYNAMIC LENGTH LIMIT 5.
          05 B2 PIC 1 USAGE BIT.
          05 TB2 PIC 1 USAGE BIT OCCURS 1 TO 16 DEPENDING ON N2.
       01 G4.
          05 DD4 PIC X DYNAMIC LENGTH LIMIT 5.
          05 B4 PIC 1(9) USAGE BIT.
          05 TB4 PIC 1 USAGE BIT OCCURS 1 TO 16 DEPENDING ON N.
       01 G5.
          05 DD5 PIC X DYNAMIC LENGTH LIMIT 5.
          05 B5 PIC 1 USAGE BIT.
          05 H5 GROUP-USAGE BIT.
             10 HB PIC 1.
             10 TB5 PIC 1 OCCURS 1 TO 16 DEPENDING ON N.
       01 GC.
          05 GD PIC X DYNAMIC LENGTH LIMIT 5.
          05 GB PIC 1 USAGE BIT.
          05 GT PIC 1 USAGE BIT OCCURS 1 TO 16 DEPENDING ON N.
       01 P USAGE POINTER.
       01 T PIC 9(4).
       01 I PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           SET P TO ADDRESS OF GC
           MOVE "ab" TO DD
           MOVE B"0" TO B
           MOVE B"1" TO TB(1) TB(3)
           MOVE B"0" TO TB(2)
           MOVE FUNCTION LENGTH(G) TO T
           DISPLAY "L3 " T
           MOVE 16 TO N
           MOVE FUNCTION LENGTH(G) TO T
           DISPLAY "LX " T
           MOVE 3 TO N
           DISPLAY "D [" G "]"
           MOVE "ef" TO DD4
           MOVE B"010000010" TO B4
           MOVE B"1" TO TB4(1) TB4(3)
           MOVE B"0" TO TB4(2)
           MOVE FUNCTION LENGTH(G4) TO T
           DISPLAY "L4 " T " [" G4 "]"
           MOVE "cd" TO DD5
           MOVE B"0" TO B5
           MOVE B"1" TO HB TB5(1) TB5(3)
           MOVE B"0" TO TB5(2)
           MOVE FUNCTION LENGTH(G5) TO T
           DISPLAY "L5 " T " [" G5 "]"
           MOVE "zz" TO DD2
           MOVE B"1" TO B2
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 16
               MOVE B"1" TO TB2(I)
           END-PERFORM
           MOVE 2 TO N2
           MOVE G TO G2
           MOVE 16 TO N2
           DISPLAY "M " DD2 " " B2 " " WITH NO ADVANCING
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 8
               DISPLAY TB2(I) WITH NO ADVANCING
           END-PERFORM
           DISPLAY " "
           MOVE "ab" TO GD
           MOVE B"0" TO GB
           MOVE B"1" TO GT(1) GT(3)
           MOVE B"0" TO GT(2)
           MOVE FUNCTION LENGTH(GC) TO T
           DISPLAY "C " T " [" GC "]"
           CALL "PB3030RUNSUB" AS NESTED USING BY REFERENCE G
           DISPLAY "AFTER " DD " " B
           PERFORM WRITE-AND-READ
           STOP RUN.
       WRITE-AND-READ.
           OPEN OUTPUT QF
           MOVE 9 TO QK
           MOVE "abc" TO QD
           MOVE B"0" TO QB
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 16
               MOVE B"0" TO QT(I)
           END-PERFORM
           MOVE B"1" TO QT(1) QT(7) QT(9)
           MOVE FUNCTION LENGTH(QREC) TO T
           DISPLAY "W9 " T
           WRITE QREC
           MOVE 3 TO QK
           MOVE "de" TO QD
           MOVE FUNCTION LENGTH(QREC) TO T
           DISPLAY "W3 " T
           WRITE QREC
           CLOSE QF
           OPEN INPUT QF
           PERFORM 2 TIMES
               MOVE 9 TO QK
               PERFORM VARYING I FROM 1 BY 1 UNTIL I > 16
                   MOVE B"1" TO QT(I)
               END-PERFORM
               READ QF
               DISPLAY "R " QK " " QD " " QB " " WITH NO ADVANCING
               PERFORM VARYING I FROM 1 BY 1 UNTIL I > QK
                   DISPLAY QT(I) WITH NO ADVANCING
               END-PERFORM
               DISPLAY " "
           END-PERFORM
           CLOSE QF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3030RUNSUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 M PIC 99 VALUE 16.
       01 J PIC 99.
       01 TL PIC 9(4).
       LINKAGE SECTION.
       01 L.
          05 LD PIC X DYNAMIC LENGTH LIMIT 5.
          05 LB PIC 1 USAGE BIT.
          05 LTB PIC 1 USAGE BIT OCCURS 1 TO 16 DEPENDING ON M.
       PROCEDURE DIVISION USING L.
           MOVE FUNCTION LENGTH(L) TO TL
           DISPLAY "IN " LD " " TL " " LB " " WITH NO ADVANCING
           PERFORM VARYING J FROM 1 BY 1 UNTIL J > 7
               DISPLAY LTB(J) WITH NO ADVANCING
           END-PERFORM
           DISPLAY " "
           MOVE "xyz" TO LD
           MOVE B"1" TO LB
           GOBACK.
       END PROGRAM PB3030RUNSUB.
       END PROGRAM PB3030RUN.
