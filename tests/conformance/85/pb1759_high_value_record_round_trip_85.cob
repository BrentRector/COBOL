       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1759RT.
      *> kb/Work PB1759 (owner decisions R51/R52; design
      *> COBOLNET_FILES_DESIGN D29) - HIGH-VALUE IS U+FFFF, THE
      *> HIGHEST OF THE 65,536 NATIVE CHARACTERS (§8.3.3.6.4 GR6:
      *> "the character ... that has the highest ordinal position in
      *> the runtime collating sequence"), AND IT CROSSES THE ONE-BYTE
      *> RECORD MEDIUM AS THE BYTE X'FF' BY THE STORAGE-BYTE LAW: BYTE
      *> X'FF' IS U+FFFF, EVERY OTHER BYTE b IS U+00bb.
      *>
      *> WHY EACH LEG CAN FAIL:
      *>  WS-EQ    - MOVE HIGH-VALUES TO A GROUP WITH A COMP FIELD,
      *>             THEN IF GROUP = HIGH-VALUES: NOTHING ALTERED THE
      *>             CHARACTERS THE MOVE STORED (§14.9.25), SO EQUAL.
      *>             A BINARY LEAF THAT RE-IMAGED ITS FF BYTES AS
      *>             U+00FF WOULD PRINT WS-NE.
      *>  SQ-*     - SEQUENTIAL: WRITE, READ BACK, STILL HIGH-VALUES;
      *>             THE COMP FIELD READS THE SAME BYTES AS -1.
      *>  RL-*     - RELATIVE, WITH A REDEFINES NUMERIC VIEW OF THE
      *>             SAME RECORD AREA: -1.
      *>  IX-*     - INDEXED: A HIGH-VALUES KEY SORTS AFTER "ZZZZ"
      *>             (§12.4.5.7 - THE KEY ORDER IS THE COLLATING
      *>             SEQUENCE), AND A RANDOM READ FINDS IT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "pb1759rt-s.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT RL ASSIGN TO "pb1759rt-r.dat"
               ORGANIZATION IS RELATIVE ACCESS IS RANDOM
               RELATIVE KEY IS RK FILE STATUS IS FS.
           SELECT IX ASSIGN TO "pb1759rt-i.dat"
               ORGANIZATION IS INDEXED ACCESS IS DYNAMIC
               RECORD KEY IS IK FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD SQ.
       01 SREC.
          05 SA PIC X(3).
          05 SB PIC S9(4) COMP.
       FD RL.
       01 RREC PIC X(6).
       01 RRECN.
          05 RN PIC S9(4) COMP.
          05 FILLER PIC X(4).
       FD IX.
       01 IREC.
          05 IK PIC X(4).
          05 IB PIC S9(4) COMP.
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 RK PIC 9(4).
       01 WREC.
          05 WA PIC X(3).
          05 WB PIC S9(4) COMP.
       PROCEDURE DIVISION.
       MAIN.
           MOVE HIGH-VALUES TO WREC.
           IF WREC = HIGH-VALUES DISPLAY "WS-EQ" ELSE DISPLAY "WS-NE".
           OPEN OUTPUT SQ.
           MOVE HIGH-VALUES TO SREC.
           WRITE SREC.
           DISPLAY "SQ-W=" FS.
           CLOSE SQ.
           OPEN INPUT SQ.
           MOVE SPACES TO SREC.
           READ SQ.
           IF SREC = HIGH-VALUES DISPLAY "SQ-R=" FS " HV"
           ELSE DISPLAY "SQ-R=" FS " NOT-HV".
           IF SB = -1 DISPLAY "SQ-B=-1" ELSE DISPLAY "SQ-B-WRONG".
           CLOSE SQ.
           OPEN OUTPUT RL.
           MOVE 1 TO RK.
           MOVE HIGH-VALUES TO RREC.
           WRITE RREC.
           DISPLAY "RL-W=" FS.
           CLOSE RL.
           OPEN INPUT RL.
           MOVE 1 TO RK.
           MOVE SPACES TO RREC.
           READ RL.
           IF RREC = HIGH-VALUES DISPLAY "RL-R=" FS " HV"
           ELSE DISPLAY "RL-R=" FS " NOT-HV".
           IF RN = -1 DISPLAY "RL-N=-1" ELSE DISPLAY "RL-N-WRONG".
           CLOSE RL.
           OPEN OUTPUT IX.
           MOVE HIGH-VALUES TO IREC.
           WRITE IREC.
           MOVE "ZZZZ" TO IK.
           MOVE 7 TO IB.
           WRITE IREC.
           DISPLAY "IX-W=" FS.
           CLOSE IX.
           OPEN INPUT IX.
           MOVE LOW-VALUES TO IK.
           START IX KEY IS NOT LESS THAN IK.
           READ IX NEXT RECORD.
           DISPLAY "IX-1=" IK " " IB.
           READ IX NEXT RECORD.
           IF IK = HIGH-VALUES DISPLAY "IX-2=HV " IB
           ELSE DISPLAY "IX-2=NOT-HV".
           MOVE HIGH-VALUES TO IK.
           READ IX KEY IS IK.
           IF IREC = HIGH-VALUES DISPLAY "IX-K=" FS " HV"
           ELSE DISPLAY "IX-K=" FS " NOT-HV".
           CLOSE IX.
           STOP RUN.
