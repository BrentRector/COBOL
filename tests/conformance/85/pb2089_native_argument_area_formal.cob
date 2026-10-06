      *> kb/Work PB2089 - ISO 1989:2023 14.2.3 GR8: "If the argument is
      *> passed by reference, the activated runtime element operates as if
      *> the formal parameter occupies the same storage area as the
      *> argument." and 13.18.44.4 GR2: "When the same storage area is
      *> defined by more than one data description entry, the data-name
      *> associated with any of those data description entries may be
      *> used to reference that storage area."
      *> cite.py --check 14.2.3 "If the argument is passed by reference,
      *>   the activated runtime element operates as if the formal
      *>   parameter occupies the same storage area as the argument"
      *>   -> OK 14.2.3 8)
      *> cite.py --check 13.18.44.4 "When the same storage area is
      *>   defined by more than one data description entry" -> OK
      *>   13.18.44.4 2)
      *> A NUMERIC argument (DISPLAY, COMP, signed) passed twice: the
      *> first formal is REDEFINED (so its description is an area), the
      *> second elementary. A store through either formal is a store to
      *> the one argument, so the other formal and the activating element
      *> see it at once. Before the fix the redefined formal held a copy
      *> taken at entry, never saw the other formal's store, and its
      *> copy-back at return undid it (S1 LK-1 0001, N 0001).
      *> The same holds when the argument is itself a formal passed on
      *> (S4 -> S5), a subscripted element (S6), and a GLOBAL item a
      *> contained program changes while the activated element is active
      *> (S7 -> S8).
      *> DERIVATION: S1 stores 7 through LK-2 -> LK-1 = 0007, LK-1X =
      *>   "0007"; then "0042" through LK-1X -> LK-2 = 0042, N = 0042,
      *>   ADD 1 -> 0043. S2: 7 through LK-2 -> LK-1 = 0007, B = 0007.
      *>   S3: -12.5 through LK-2 (S9(3)V9 SIGN LEADING SEPARATE) ->
      *>   LK-1 = -0125, S = -0125. S4 stores 1 in LF then S5 stores 4 in
      *>   LC-2 -> LC-1 = 4, LF = 4, C = 4. S6: 42 through LT-2 of TE(2)
      *>   -> LT-1 = 42, TT = 114211. S7: S8 stores 9 in G-N -> LK-7 =
      *>   0009, G-N = 0009.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2089M85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 1.
       01 B PIC 9(4) COMP VALUE 1.
       01 S PIC S9(3)V9 SIGN LEADING SEPARATE VALUE -1.5.
       01 C PIC 9 VALUE 2.
       01 I PIC 9 VALUE 2.
       01 TT.
          05 TE PIC 99 OCCURS 3.
       01 G-N PIC 9(4) GLOBAL VALUE 5.
       PROCEDURE DIVISION.
           CALL "P2089S1" USING N N
           DISPLAY "S1 N " N
           ADD 1 TO N
           DISPLAY "S1 N+1 " N
           CALL "P2089S2" USING B B
           DISPLAY "S2 B " B
           CALL "P2089S3" USING S S
           DISPLAY "S3 S " S
           CALL "P2089S4" USING C
           DISPLAY "S4 C " C
           MOVE "111111" TO TT
           CALL "P2089S6" USING TE (I) TE (I)
           DISPLAY "S6 TT " TT
           CALL "P2089S7" USING G-N
           DISPLAY "S7 G-N " G-N
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2089S7.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-7 PIC 9(4).
       01 LK-7X REDEFINES LK-7 PIC X(4).
       PROCEDURE DIVISION USING LK-7.
           CALL "P2089S8"
           DISPLAY "S7 LK-7 " LK-7
           EXIT PROGRAM.
       END PROGRAM P2089S7.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2089S8 IS COMMON.
       PROCEDURE DIVISION.
           MOVE 9 TO G-N
           EXIT PROGRAM.
       END PROGRAM P2089S8.
       END PROGRAM P2089M85.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2089S1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-1 PIC 9(4).
       01 LK-1X REDEFINES LK-1 PIC X(4).
       01 LK-2 PIC 9(4).
       PROCEDURE DIVISION USING LK-1 LK-2.
           MOVE 7 TO LK-2
           DISPLAY "S1 LK-1 " LK-1
           DISPLAY "S1 LK-1X " LK-1X
           MOVE "0042" TO LK-1X
           DISPLAY "S1 LK-2 " LK-2
           EXIT PROGRAM.
       END PROGRAM P2089S1.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2089S2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-1 PIC 9(4) COMP.
       01 LK-1X REDEFINES LK-1 PIC X(2).
       01 LK-2 PIC 9(4) COMP.
       PROCEDURE DIVISION USING LK-1 LK-2.
           MOVE 7 TO LK-2
           DISPLAY "S2 LK-1 " LK-1
           EXIT PROGRAM.
       END PROGRAM P2089S2.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2089S3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-1 PIC S9(3)V9 SIGN LEADING SEPARATE.
       01 LK-1X REDEFINES LK-1 PIC X(5).
       01 LK-2 PIC S9(3)V9 SIGN LEADING SEPARATE.
       PROCEDURE DIVISION USING LK-1 LK-2.
           MOVE -12.5 TO LK-2
           DISPLAY "S3 LK-1 " LK-1
           EXIT PROGRAM.
       END PROGRAM P2089S3.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2089S4.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LF PIC 9.
       PROCEDURE DIVISION USING LF.
           MOVE 1 TO LF
           CALL "P2089S5" USING LF LF
           DISPLAY "S4 LF " LF
           EXIT PROGRAM.
       END PROGRAM P2089S4.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2089S5.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LC-1 PIC 9.
       01 LC-1X REDEFINES LC-1 PIC X.
       01 LC-2 PIC 9.
       PROCEDURE DIVISION USING LC-1 LC-2.
           MOVE 4 TO LC-2
           DISPLAY "S5 LC-1 " LC-1
           EXIT PROGRAM.
       END PROGRAM P2089S5.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2089S6.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LT-1 PIC 99.
       01 LT-1X REDEFINES LT-1 PIC XX.
       01 LT-2 PIC 99.
       PROCEDURE DIVISION USING LT-1 LT-2.
           MOVE 42 TO LT-2
           DISPLAY "S6 LT-1 " LT-1
           EXIT PROGRAM.
       END PROGRAM P2089S6.
