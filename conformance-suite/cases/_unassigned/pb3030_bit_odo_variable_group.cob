      *>--- conformance-case
      *> id: pb3030_bit_odo_variable_group
      *> rules: []
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3030 - a VARIABLE-LENGTH GROUP whose OCCURS DEPENDING
      *> table holds USAGE BIT items: its current extent, its MOVE,
      *> its crossing of an activation boundary and its file record.
      *>
      *> 8.5.1.6.3: a bit item following a bit item of the same level
      *>   is placed "at the next bit position in storage", so B and
      *>   the occurrences of TB share bytes and H's extent is counted
      *>   in bits. 13.18.38.4 GR8 a): data-name-1 is outside each
      *>   group, so "only that part of the table area that is
      *>   specified by the value of the data item referenced by
      *>   data-name-1 at the start of the operation will be used",
      *>   sending or receiving, and a receiver's occurrences past it
      *>   keep their content.
      *> 15.50.4 rule 7: a variable-length group's LENGTH is the sum of
      *>   its non-variable-length subordinates, the current lengths of
      *>   its dynamic-length items and its dynamic-capacity tables at
      *>   their current capacity; rule 9 rounds a part of a character
      *>   up. 15.14.4 rule 6 is BYTE-LENGTH's twin.
      *> This group was the named stage COBOLNET1756 and aborted the
      *>   run unit before (a table of runtime-length elements beside
      *>   a bit OCCURS DEPENDING table); the cell-backed and record
      *>   forms were never composed.
      *>
      *> Expected values, derived by hand (bits listed B then TB(1..)):
      *>   L2  TD holds DD(1) = "ab" (2) and H holds 1 + 2 bits, one
      *>       character: 2 + 1 = 3, for LENGTH and BYTE-LENGTH alike.
      *>   L8  1 + 8 = 9 bits, two characters: 2 + 2 = 4.
      *>   L7  1 + 7 = 8 bits, one character: 3.
      *>   D7  B = 0 and TB = 1000001 pack into 01000001, "A": [abA].
      *>   M   G2 holds 1 then eight 1s; MOVE G TO G2 with N2 = 2 takes
      *>       only B and TB2(1..2) from G (0, 1, 0) and keeps the rest:
      *>       DD2(1) ab, bits 0 10111111.
      *>   C   the same receive into a CELL-BACKED group (ADDRESS OF
      *>       is taken): G3 holds all 1s, N3 = 2, MOVE GC TO G3 with
      *>       GC at N = 2 (GD "ab" and one character, "A"):
      *>       ab 0 10111111.
      *>   IN  the nested program's formal L is G at its maximum
      *>       (14.8.2.2, "the maximum length is used"): M = 8, so
      *>       2 + 2 = 4, bits 0 1000001 for B and LTB(1..7).
      *>   AFTER  BY REFERENCE: the callee's MOVEs are G's: xyz, B 1.
      *>   R   a record written at QK = 9 (QX "X", QB 0, QT 1..9 =
      *>       100000101) and at QK = 3 (QT 100) reads back the same,
      *>       the record's length saying its CHARACTER extent: W9 is
      *>       1 + 3 + 1 + 2 = 7, W3 is 1 + 2 + 1 + 1 = 5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3030BIT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT QF ASSIGN TO "pb3030.seq" ORGANIZATION SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD QF.
       01 QREC.
          05 QK PIC 9.
          05 QD PIC X DYNAMIC LENGTH LIMIT 5.
          05 QH.
             10 QX PIC X.
             10 QB PIC 1 USAGE BIT.
             10 QT PIC 1 USAGE BIT OCCURS 1 TO 16 DEPENDING ON QK.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 2.
       01 N2 PIC 9 VALUE 8.
       01 N3 PIC 9 VALUE 8.
       01 G.
          05 TD OCCURS DYNAMIC CAPACITY IN CP.
             10 DD PIC X DYNAMIC LENGTH LIMIT 5.
          05 H.
             10 B PIC 1 USAGE BIT.
             10 TB PIC 1 USAGE BIT OCCURS 1 TO 8 DEPENDING ON N.
       01 G2.
          05 TD2 OCCURS DYNAMIC CAPACITY IN CP2.
             10 DD2 PIC X DYNAMIC LENGTH LIMIT 5.
          05 H2.
             10 B2 PIC 1 USAGE BIT.
             10 TB2 PIC 1 USAGE BIT OCCURS 1 TO 8 DEPENDING ON N2.
       01 G3.
          05 DD3 PIC X DYNAMIC LENGTH LIMIT 5.
          05 H3.
             10 B3 PIC 1 USAGE BIT.
             10 TB3 PIC 1 USAGE BIT OCCURS 1 TO 8 DEPENDING ON N3.
       01 GC.
          05 GD PIC X DYNAMIC LENGTH LIMIT 5.
          05 GH.
             10 GB PIC 1 USAGE BIT.
             10 GT PIC 1 USAGE BIT OCCURS 1 TO 8 DEPENDING ON N.
       01 P USAGE POINTER.
       01 T PIC 9(4).
       01 I PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           SET P TO ADDRESS OF G3
           MOVE "ab" TO DD(1)
           MOVE FUNCTION LENGTH(G) TO T
           DISPLAY "L2 " T WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G) TO T
           DISPLAY " " T
           MOVE 8 TO N
           MOVE FUNCTION LENGTH(G) TO T
           DISPLAY "L8 " T
           MOVE 7 TO N
           MOVE FUNCTION LENGTH(G) TO T
           DISPLAY "L7 " T
           MOVE B"0" TO B
           MOVE B"1" TO TB(1) TB(7)
           MOVE B"0" TO TB(2) TB(3) TB(4) TB(5) TB(6)
           DISPLAY "D7 [" G "]"
           MOVE B"1" TO B2 B3
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 8
               MOVE B"1" TO TB2(I) TB3(I)
           END-PERFORM
           MOVE 2 TO N2
           MOVE G TO G2
           MOVE 8 TO N2
           DISPLAY "M " DD2(1) " " B2 " " WITH NO ADVANCING
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 8
               DISPLAY TB2(I) WITH NO ADVANCING
           END-PERFORM
           DISPLAY " "
           MOVE 7 TO N
           MOVE "ab" TO GD
           MOVE B"0" TO GB
           MOVE B"1" TO GT(1) GT(7)
           MOVE B"0" TO GT(2) GT(3) GT(4) GT(5) GT(6)
           MOVE 2 TO N N3
           MOVE GC TO G3
           MOVE 8 TO N3
           DISPLAY "C " DD3 " " B3 " " WITH NO ADVANCING
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 8
               DISPLAY TB3(I) WITH NO ADVANCING
           END-PERFORM
           DISPLAY " "
           MOVE 7 TO N
           CALL "PB3030SUB" AS NESTED USING BY REFERENCE GC
           DISPLAY "AFTER " GD " " GB
           PERFORM WRITE-AND-READ
           STOP RUN.
       WRITE-AND-READ.
           OPEN OUTPUT QF
           MOVE 9 TO QK
           MOVE "abc" TO QD
           MOVE "X" TO QX
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
               DISPLAY "R " QK " " QD " " QX " " QB " "
                   WITH NO ADVANCING
               PERFORM VARYING I FROM 1 BY 1 UNTIL I > QK
                   DISPLAY QT(I) WITH NO ADVANCING
               END-PERFORM
               DISPLAY " "
           END-PERFORM
           CLOSE QF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3030SUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 M PIC 9 VALUE 8.
       01 J PIC 9.
       01 TL PIC 9(4).
       LINKAGE SECTION.
       01 L.
          05 LD PIC X DYNAMIC LENGTH LIMIT 5.
          05 LH.
             10 LB PIC 1 USAGE BIT.
             10 LTB PIC 1 USAGE BIT OCCURS 1 TO 8 DEPENDING ON M.
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
       END PROGRAM PB3030SUB.
       END PROGRAM PB3030BIT.
