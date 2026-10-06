      *> kb/Work PB2087 (the INVOKE arm) - ISO 1989:2023 14.2.3 GR8: "If
      *> the argument is passed by reference, the activated runtime
      *> element operates as if the formal parameter occupies the same
      *> storage area as the argument." A METHOD's formals obey it as a
      *> program's do: two formals passed the same argument (a group, an
      *> elementary numeric), a REDEFINED formal over a numeric argument,
      *> a method formal passed on to a CALL and to an INVOKE, and every
      *> argument of an INVOKE through a universal object reference
      *> (14.9.23.3 SR6 makes each one BY REFERENCE) all see every store
      *> at once. BY CONTENT (GR9) is the contrast: its own record.
      *> cite.py --check 14.2.3 "If the argument is passed by reference,
      *>   the activated runtime element operates as if the formal
      *>   parameter occupies the same storage area as the argument"
      *>   -> OK 14.2.3 8)
      *> cite.py --check 14.9.23.4 "if the argument meets the requirements
      *>   of Syntax rules 9 and 10, BY REFERENCE is assumed" -> OK
      *>   14.9.23.4 6)
      *> Before the fix every method formal was copied in at entry and
      *> out at return: G G printed LK-2 F1 1 and AFTER 1XYZ (the second
      *> copy-back undid the first formal's store), N N LN-1 0001.
      *> DERIVATION: MR stores 7 through LR-2 -> LR-1 = 0007, LR-1X =
      *>   "0007"; "0042" through LR-1X -> LR-2 = 0042, N = 0042. MF
      *>   stores 1 in LF; the CALL's LC-2 store of 4 is LC-1's and LF's;
      *>   then 2, and MQ's LQ-2 store of 6 is LQ-1's and LF's: C = 6. MC
      *>   BY CONTENT: LK-2 F1 stays 1, WS-REC stays 1ABC. MO: LK-1's 8
      *>   reaches WS-REC. MG (typed and universal): 5 through LK-1 is
      *>   LK-2's, XYZ through LK-2 is LK-1's -> 5XYZ. MN: 7 through
      *>   LN-2 is LN-1's and N's.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087I02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P2087K02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REC.
          05 F1 PIC 9 VALUE 1.
          05 F2 PIC X(3) VALUE "ABC".
       01 N PIC 9(4) VALUE 1.
       01 C PIC 9 VALUE 1.
       01 O USAGE OBJECT REFERENCE P2087K02.
       01 U USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
           INVOKE P2087K02 "NEW" RETURNING O
           SET U TO O
           INVOKE O "MR" USING N N
           DISPLAY "MR N " N
           INVOKE O "MF" USING C
           DISPLAY "MF C " C
           INVOKE O "MC" USING BY CONTENT WS-REC WS-REC
           DISPLAY "MC AFTER " WS-REC
           INVOKE O "MO" USING WS-REC OMITTED
           DISPLAY "MO AFTER " WS-REC
           MOVE "1ABC" TO WS-REC
           MOVE 1 TO N
           INVOKE O "MG" USING WS-REC WS-REC
           DISPLAY "TG AFTER " WS-REC
           INVOKE O "MN" USING N N
           DISPLAY "TN N " N
           MOVE "1ABC" TO WS-REC
           MOVE 1 TO N
           INVOKE U "MG" USING WS-REC WS-REC
           DISPLAY "UG AFTER " WS-REC
           INVOKE U "MN" USING N N
           DISPLAY "UN N " N
           STOP RUN.
       END PROGRAM P2087I02.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087S02.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LC-1 PIC 9.
       01 LC-1X REDEFINES LC-1 PIC X.
       01 LC-2 PIC 9.
       PROCEDURE DIVISION USING LC-1 LC-2.
           MOVE 4 TO LC-2
           DISPLAY "S LC-1 " LC-1
           GOBACK.
       END PROGRAM P2087S02.

       IDENTIFICATION DIVISION.
       CLASS-ID. P2087K02 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR-1 PIC 9(4).
       01 LR-1X REDEFINES LR-1 PIC X(4).
       01 LR-2 PIC 9(4).
       PROCEDURE DIVISION USING LR-1 LR-2.
           MOVE 7 TO LR-2
           DISPLAY "MR LR-1 " LR-1 " " LR-1X
           MOVE "0042" TO LR-1X
           DISPLAY "MR LR-2 " LR-2
           GOBACK.
       END METHOD MR.
       METHOD-ID. MF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LF PIC 9.
       PROCEDURE DIVISION USING LF.
           MOVE 1 TO LF
           CALL "P2087S02" USING LF LF
           DISPLAY "MF LF " LF
           MOVE 2 TO LF
           INVOKE SELF "MQ" USING LF LF
           DISPLAY "MF LF2 " LF
           GOBACK.
       END METHOD MF.
       METHOD-ID. MQ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LQ-1 PIC 9.
       01 LQ-1X REDEFINES LQ-1 PIC X.
       01 LQ-2 PIC 9.
       PROCEDURE DIVISION USING LQ-1 LQ-2.
           MOVE 6 TO LQ-2
           DISPLAY "MQ LQ-1 " LQ-1
           GOBACK.
       END METHOD MQ.
       METHOD-ID. MC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-1.
          05 L1F1 PIC 9.
          05 L1F2 PIC X(3).
       01 LK-2.
          05 L2F1 PIC 9.
          05 L2F2 PIC X(3).
       PROCEDURE DIVISION USING LK-1 LK-2.
           MOVE 5 TO L1F1
           DISPLAY "MC LK-2 F1 " L2F1
           GOBACK.
       END METHOD MC.
       METHOD-ID. MO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-1.
          05 L1F1 PIC 9.
          05 L1F2 PIC X(3).
       01 LK-2.
          05 L2F1 PIC 9.
          05 L2F2 PIC X(3).
       PROCEDURE DIVISION USING LK-1 OPTIONAL LK-2.
           MOVE 8 TO L1F1
           IF LK-2 IS OMITTED DISPLAY "MO LK-2 OMITTED" END-IF
           GOBACK.
       END METHOD MO.
       METHOD-ID. MG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-1.
          05 L1F1 PIC 9.
          05 L1F2 PIC X(3).
       01 LK-2.
          05 L2F1 PIC 9.
          05 L2F2 PIC X(3).
       PROCEDURE DIVISION USING LK-1 LK-2.
           MOVE 5 TO L1F1
           DISPLAY "G LK-2 F1 " L2F1
           MOVE "XYZ" TO L2F2
           DISPLAY "G LK-1 F2 " L1F2
           GOBACK.
       END METHOD MG.
       METHOD-ID. MN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN-1 PIC 9(4).
       01 LN-2 PIC 9(4).
       PROCEDURE DIVISION USING LN-1 LN-2.
           MOVE 7 TO LN-2
           DISPLAY "N LN-1 " LN-1
           GOBACK.
       END METHOD MN.
       END OBJECT.
       END CLASS P2087K02.
