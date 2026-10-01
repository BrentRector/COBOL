      *> ISO/IEC 1989:2023 14.9.25.4 GR4 (kb/Work PB1665): a group move is "treated exactly as if it were an
      *> alphanumeric to alphanumeric elementary move" - the whole sending area arrives. A group holding a national
      *> leaf is an ALPHANUMERIC group (8.5.2.1), whose characters are its STORAGE characters, a national position
      *> being two (13.18.60.4 GR8, pinned by the implementor, D-N1). The width of the group's image is one quantity
      *> for every consumer: the MOVE, the comparison (8.8.4.2.12), the CALL boundary (14.8.2.2) and the RENAMES
      *> alias (13.18.45.4 GR2), so no trailing member is lost.
      *> WHY EACH LINE CAN FAIL (a position-counted width drops the trailing X(2) of the receiver):
      *>   M=   [AB][CD][EF]  MOVE G5 TO G6 - the trailing B survives.
      *>   EQ   the two groups then compare equal.
      *>   RT=  [AB][CD][EF]  through an 8-character alphanumeric item and back.
      *>   S1=, R1=           the formal group and the caller's receiving group, across CALL ... BY REFERENCE.
      *>   RM=[EF]            positions 7-8 of the group are its last two characters (8.4.3.3.4 GR5: the group is
      *>                      an alphanumeric item of 8 character positions - the old count, 6, refused the position).
      *>   NG=[L][R], NN=[12][3]  a national GROUP inside an alphanumeric group moves whole; its own positions (3)
      *>                      stay national positions while its parent counts the 6 characters it occupies.
      *>   LEN=0009, TAIL=[FG ]  the alias over P1 THRU P2 is 2 + 5 + 2 characters and its positions 8-9 are P2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1665GROUPNAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G5.
          05 A PIC X(2) VALUE "AB".
          05 N PIC N(2) VALUE N"CD".
          05 B PIC X(2) VALUE "EF".
       01 G6.
          05 A PIC X(2).
          05 N PIC N(2).
          05 B PIC X(2).
       01 G7.
          05 A PIC X(2).
          05 N PIC N(2).
          05 B PIC X(2).
       01 G8.
          05 A PIC X(2).
          05 N PIC N(2).
          05 B PIC X(2).
       01 W8 PIC X(8).
       01 NG1.
          05 NA PIC X.
          05 NNG GROUP-USAGE NATIONAL.
             10 NN1 PIC N(2).
             10 NN2 PIC N.
          05 NB PIC X.
       01 NG2.
          05 NA PIC X.
          05 NNG GROUP-USAGE NATIONAL.
             10 NN1 PIC N(2).
             10 NN2 PIC N.
          05 NB PIC X.
       01 REC.
          05 P1 PIC X(2) VALUE "AB".
          05 G.
             10 GN PIC N(2) VALUE N"CD".
             10 GX PIC X VALUE "E".
          05 P2 PIC X(2) VALUE "FG".
          66 RA RENAMES P1 THRU P2.
       01 WIDE PIC X(20).
       01 LEN PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE G5 TO G6
           DISPLAY "M=[" A OF G6 "][" N OF G6 "][" B OF G6 "]"
           IF G5 = G6 DISPLAY "EQ" ELSE DISPLAY "NE" END-IF
           MOVE G5 TO W8
           MOVE W8 TO G7
           DISPLAY "RT=[" A OF G7 "][" N OF G7 "][" B OF G7 "]"
           CALL "PB1665SUB" USING G5 G8
           DISPLAY "R1=[" A OF G8 "][" N OF G8 "][" B OF G8 "]"
           DISPLAY "RM=[" G5(7:2) "]"
           MOVE "L" TO NA OF NG1
           MOVE N"12" TO NN1 OF NG1
           MOVE N"3" TO NN2 OF NG1
           MOVE "R" TO NB OF NG1
           MOVE NG1 TO NG2
           DISPLAY "NG=[" NA OF NG2 "][" NB OF NG2 "]"
           DISPLAY "NN=[" NN1 OF NG2 "][" NN2 OF NG2 "]"
           MOVE FUNCTION LENGTH(RA) TO LEN
           DISPLAY "LEN=" LEN
           MOVE RA TO WIDE
           DISPLAY "TAIL=[" WIDE(8:3) "]"
           STOP RUN.
       END PROGRAM PB1665GROUPNAT.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1665SUB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1.
          05 LA PIC X(2).
          05 LN PIC N(2).
          05 LB PIC X(2).
       01 L2.
          05 MA PIC X(2).
          05 MN PIC N(2).
          05 MB PIC X(2).
       PROCEDURE DIVISION USING L1 L2.
       SUBMAIN.
           DISPLAY "S1=[" LA "][" LN "][" LB "]"
           MOVE L1 TO L2
           GOBACK.
       END PROGRAM PB1665SUB.
