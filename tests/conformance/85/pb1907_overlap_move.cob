      *> ISO 14.6.10 1) - OVERLAPPING OPERANDS OF MOVE (AND ADD): AN UNDEFINED
      *> RESULT THAT WISEOWL COBOL DOCUMENTS AND PINS (kb/Work PB1907; D-OVL1
      *> and D-OVL2 in docs/CONFORMANCE.md section 3).
      *>
      *> THE RULES.
      *> 14.6.10 1): "When the data items are not described by the same data
      *>   description entry, the result of the statement is undefined."  OK
      *>   cite.py --check 14.6.10 -> 1).
      *> 14.6.10 2): "When the data items are described by the same data
      *>   description entry, the result of the statement is the same as if
      *>   the data items shared no part of their respective storage areas."
      *>   OK cite.py --check 14.6.10 -> 2).
      *> A.2 item 36: "Overlapping operands. The situations where the results
      *>   of an operation involving overlapping operands are undefined ..."
      *>   with b) "When one or more of the operands is reference-modified."
      *>   OK cite.py --check A.2 -> 36).
      *> 4.4 2): "A COBOL run unit that allows these situations to happen is a
      *>   conforming run unit" - so every result below conforms, and the
      *>   expected output is WiseOwl COBOL's documented choice, not a value
      *>   the standard derives.  OK cite.py --check 4.4 -> 2).
      *>
      *> THE DOCUMENTED CHOICE (CLAUDE.md rule 1: the ISO text is silent, so
      *> GnuCOBOL 3.2.0 decides, MEASURED with a built cobc, not read from
      *> its source).
      *>   SNAPSHOT: a MOVE reads its whole sender before it writes its
      *>   receiver: RM-*, RD-*, GRP-SUB, SUB-GRP, JUST-EQ (equal sizes,
      *>   no pad), NUM-SAME (identical descriptions), and ADD's NUM-ADD.
      *>   D-OVL2 (a): a JUSTIFIED RIGHT receiver with a shorter
      *>   alphanumeric sender has its leading pad positions space-filled
      *>   BEFORE the sender is read: JUST, JUST-SELF, JUST-GRP, JUST-IN,
      *>   JUST-LEAD.
      *>   D-OVL2 (b): a numeric DISPLAY receiver is set to zero BEFORE the
      *>   digits are taken from a differently described numeric DISPLAY
      *>   sender or from an alphanumeric sender: NUM-MOVE, NUM-SELF,
      *>   ALNUM-NUM, SLICE-NUM, NUM-NUM, NUM-SCALE.
      *> Both corners apply only where the overlap is a compile-time fact.
      *>
      *> WHY EACH LEG CAN FAIL: a compiler that snapshots every MOVE prints
      *> JUST=[A AB] and NUM-MOVE=[0234]; one that zero-fills or pads every
      *> MOVE would break the snapshot lines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1907OV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X     PIC X(6) VALUE "ABCDEF".
       01 Y     PIC X(6) VALUE "ABCDEF".
       01 R.
          05 R1 PIC X(6) VALUE "ABCDEF".
       01 RR REDEFINES R.
          05 FILLER PIC X.
          05 R2 PIC X(5).
       01 S.
          05 S1 PIC X(6) VALUE "ABCDEF".
       01 SS REDEFINES S.
          05 FILLER PIC X.
          05 S2 PIC X(5).
       01 G.
          05 GA PIC X(3) VALUE "ABC".
          05 GB PIC X(3) VALUE "DEF".
       01 J.
          05 J1 PIC X(4) VALUE "ABCD".
       01 JJ REDEFINES J.
          05 FILLER PIC X.
          05 J2 PIC X(3) JUSTIFIED RIGHT.
       01 N.
          05 N1 PIC 9(4) VALUE 1234.
       01 NN REDEFINES N.
          05 FILLER PIC X.
          05 N2 PIC 9(3).
       01 M.
          05 M1 PIC 9(4) VALUE 1234.
       01 MM REDEFINES M.
          05 FILLER PIC X.
          05 M2 PIC 9(3).
       01 JR    PIC X(4) JUSTIFIED RIGHT VALUE "ABCD".
       01 NR    PIC 9(4) VALUE 1234.
       01 Q     PIC X(5) VALUE "ABCDE".
       01 QR REDEFINES Q.
          05 FILLER PIC X.
          05 QJ PIC X(4) JUSTIFIED RIGHT.
       01 QG REDEFINES Q.
          05 QGG.
             10 QA PIC X.
             10 QB PIC X.
             10 QC PIC X.
       01 K.
          05 K1 PIC X(4) VALUE "ABCD".
       01 KK REDEFINES K.
          05 FILLER PIC X.
          05 K2 PIC X(3) JUSTIFIED RIGHT.
       01 L.
          05 L1 PIC X(6) VALUE "ABCDEF".
       01 LL REDEFINES L.
          05 FILLER PIC X.
          05 L2 PIC X(4) JUSTIFIED RIGHT.
       01 C.
          05 C1 PIC X(5) VALUE "12345".
       01 CC REDEFINES C.
          05 C2 PIC 9(3).
          05 FILLER PIC XX.
       01 F.
          05 F1 PIC 9(4) VALUE 1234.
       01 FF REDEFINES F.
          05 FILLER PIC X.
          05 F2 PIC 9(3).
       01 E.
          05 E1 PIC 9(4) VALUE 1234.
       01 EE REDEFINES E.
          05 E2 PIC 9(3)V9.
       01 A.
          05 A1 PIC 9(2) VALUE 12.
          05 A1B PIC 9 VALUE 3.
       01 AA REDEFINES A.
          05 FILLER PIC X.
          05 A2 PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE X(1:5) TO X(2:5)
           DISPLAY "RM-FWD=[" X "]"
           MOVE "ABCDEF" TO X
           MOVE X(2:5) TO X(1:5)
           DISPLAY "RM-BWD=[" X "]"
           MOVE R1 TO R2
           DISPLAY "RD-FWD=[" R1 "]"
           MOVE S2 TO S1
           DISPLAY "RD-BWD=[" S1 "]"
           MOVE G TO GB
           DISPLAY "GRP-SUB=[" G "]"
           MOVE "ABCDEF" TO G
           MOVE GB TO G
           DISPLAY "SUB-GRP=[" G "]"
           MOVE Y(1:3) TO Y(2:5)
           DISPLAY "RM-PAD=[" Y "]"
           MOVE J1(1:2) TO J2
           DISPLAY "JUST=[" J "]"
           MOVE N2 TO N1
           DISPLAY "NUM-MOVE=[" N "]"
           ADD M2 TO M1
           DISPLAY "NUM-ADD=[" M "]"
           MOVE JR(1:2) TO JR
           DISPLAY "JUST-SELF=[" JR "]"
           MOVE NR(1:3) TO NR
           DISPLAY "NUM-SELF=[" NR "]"
           MOVE QGG TO QJ
           DISPLAY "JUST-GRP=[" Q "]"
           MOVE K1(2:2) TO K2
           DISPLAY "JUST-IN=[" K "]"
           MOVE L1(4:2) TO L2
           DISPLAY "JUST-LEAD=[" L "]"
           MOVE "ABCD" TO J1
           MOVE J1(1:3) TO J2
           DISPLAY "JUST-EQ=[" J "]"
           MOVE C1 TO C2
           DISPLAY "ALNUM-NUM=[" C "]"
           MOVE "12345" TO C1
           MOVE C1(1:3) TO C2
           DISPLAY "SLICE-NUM=[" C "]"
           MOVE F2 TO F1
           DISPLAY "NUM-NUM=[" F "]"
           MOVE E1 TO E2
           DISPLAY "NUM-SCALE=[" E "]"
           MOVE A1 TO A2
           DISPLAY "NUM-SAME=[" A "]"
           STOP RUN.
