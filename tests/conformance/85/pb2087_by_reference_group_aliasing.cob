      *> kb/Work PB2087 - ISO 1989:2023 14.2.3 GR8: "If the argument is
      *> passed by reference, the activated runtime element operates as if
      *> the formal parameter occupies the same storage area as the
      *> argument." A GROUP formal, a formal another LINKAGE entry
      *> REDEFINES, and a group formal over an elementary or
      *> reference-modified argument must therefore see every store to the
      *> argument's storage AT ONCE - through a second formal passed the
      *> same argument, through an EXTERNAL record that is also the
      *> argument, and from a contained program reaching a GLOBAL formal.
      *> Before the fix each such formal held a copy taken at entry and
      *> written back at return, so the second formal printed the old
      *> value and the last copy back silently undid the first.
      *> BY CONTENT (GR9) is the contrast: each argument is its own record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087M85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REC.
          05 F1 PIC 9.
          05 F2 PIC X(3).
       01 WS-ELEM PIC X(4) VALUE "ELEM".
       01 EXT-REC EXTERNAL.
          05 E1 PIC X(4).
       01 TBL.
          05 ENT OCCURS 3.
             10 EA PIC X(2).
             10 EB PIC 9(2).
       PROCEDURE DIVISION.
           MOVE 1 TO F1
           MOVE "ABC" TO F2
           CALL "P2087S1" USING WS-REC WS-REC
           DISPLAY "S1 AFTER " WS-REC
           MOVE "1ABC" TO WS-REC
           CALL "P2087S2" USING WS-REC WS-REC
           DISPLAY "S2 AFTER " WS-REC
           CALL "P2087S2" USING WS-ELEM WS-ELEM
           DISPLAY "S2 ELEM AFTER " WS-ELEM
           MOVE "AA01BB02CC03" TO TBL
           CALL "P2087S3" USING ENT (2) TBL
           DISPLAY "S3 AFTER " TBL
           MOVE "ABCDEFGHIJKL" TO TBL
           CALL "P2087S4" USING TBL (3:4) TBL
           DISPLAY "S4 AFTER " TBL
           MOVE "QQQQ" TO E1
           CALL "P2087S5" USING EXT-REC
           DISPLAY "S5 AFTER " EXT-REC
           MOVE "3ABC" TO WS-REC
           CALL "P2087S6" USING WS-REC WS-REC
           DISPLAY "S6 AFTER " WS-REC
           MOVE "9XYZ" TO WS-REC
           CALL "P2087S8" USING BY CONTENT WS-REC WS-REC
           DISPLAY "S8 AFTER " WS-REC
           STOP RUN.
       END PROGRAM P2087M85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087S1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-1.
          05 F1 PIC 9.
          05 F2 PIC X(3).
       01 LK-2.
          05 F1 PIC 9.
          05 F2 PIC X(3).
       PROCEDURE DIVISION USING LK-1 LK-2.
           MOVE 5 TO F1 OF LK-1
           DISPLAY "S1 LK-2 F1 " F1 OF LK-2
           MOVE "XYZ" TO F2 OF LK-2
           DISPLAY "S1 LK-1 F2 " F2 OF LK-1
           EXIT PROGRAM.
       END PROGRAM P2087S1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087S2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-1 PIC X(4).
       01 LK-1R REDEFINES LK-1.
          05 R1 PIC 9.
          05 R2 PIC X(3).
       01 LK-2.
          05 S1 PIC 9.
          05 S2 PIC X(3).
       PROCEDURE DIVISION USING LK-1 LK-2.
           MOVE "8" TO R1
           DISPLAY "S2 LK-2 " LK-2
           EXIT PROGRAM.
       END PROGRAM P2087S2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087S3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LE.
          05 LA PIC X(2).
          05 LB PIC 9(2).
       01 LT.
          05 LENT OCCURS 3.
             10 LTA PIC X(2).
             10 LTB PIC 9(2).
       PROCEDURE DIVISION USING LE LT.
           MOVE "XX" TO LA
           MOVE 99 TO LB
           DISPLAY "S3 LT(2) " LENT (2)
           EXIT PROGRAM.
       END PROGRAM P2087S3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087S4.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LW.
          05 W1 PIC X(2).
          05 W2 PIC X(2).
       01 LT PIC X(12).
       PROCEDURE DIVISION USING LW LT.
           MOVE "12" TO W2
           DISPLAY "S4 LT " LT
           EXIT PROGRAM.
       END PROGRAM P2087S4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087S5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EXT-REC EXTERNAL.
          05 E1 PIC X(4).
       LINKAGE SECTION.
       01 LK.
          05 L1 PIC X(4).
       PROCEDURE DIVISION USING LK.
           MOVE "RRRR" TO L1
           DISPLAY "S5 EXTERNAL SEES " E1
           MOVE "SSSS" TO E1
           DISPLAY "S5 LK SEES " L1
           EXIT PROGRAM.
       END PROGRAM P2087S5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087S6.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-1 GLOBAL.
          05 N1 PIC 9.
          05 N2 PIC X(3).
       01 LK-2.
          05 N3 PIC 9.
          05 N4 PIC X(3).
       PROCEDURE DIVISION USING LK-1 LK-2.
           CALL "P2087S7"
           DISPLAY "S6 LK-2 " LK-2
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087S7.
       PROCEDURE DIVISION.
           MOVE 7 TO N1
           EXIT PROGRAM.
       END PROGRAM P2087S7.
       END PROGRAM P2087S6.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087S8.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LZ1.
          05 Z1 PIC X(4).
       01 LZ2.
          05 Z2 PIC X(4).
       PROCEDURE DIVISION USING LZ1 LZ2.
           MOVE "CCCC" TO Z1
           DISPLAY "S8 LZ2 " Z2
           EXIT PROGRAM.
       END PROGRAM P2087S8.
