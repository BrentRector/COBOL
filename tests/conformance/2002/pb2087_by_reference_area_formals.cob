      *> kb/Work PB2087 - ISO 1989:2023 14.2.3 GR8: "If the argument is
      *> passed by reference, the activated runtime element operates as if
      *> the formal parameter occupies the same storage area as the
      *> argument" - the COBOL 2002 surfaces of the one rule:
      *>  - a RECURSIVE activating element re-entered while the activated
      *>    one is active reads its own record, already updated (callback);
      *>  - an OMITTED argument leaves the other formals aliased;
      *>  - a user-defined function's two group formals over one argument;
      *>  - ADDRESS OF the formal IS the argument's address (8.4.3.11.4 GR1);
      *>  - a strongly-typed group with a POINTER leaf crosses whole, its
      *>    managed slot included (the CALL lane refused it before);
      *>  - a RECURSIVE program's WORKING-STORAGE record held in a static
      *>    cell is seeded with its VALUE image (it used to fail its type
      *>    initializer).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. P2087FN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LF-1.
          05 Q1 PIC 9.
          05 Q2 PIC X(3).
       01 LF-2.
          05 Q3 PIC 9.
          05 Q4 PIC X(3).
       01 LF-R PIC X(4).
       PROCEDURE DIVISION USING LF-1 LF-2 RETURNING LF-R.
           MOVE 5 TO Q1
           MOVE LF-2 TO LF-R
           GOBACK.
       END FUNCTION P2087FN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087M02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION P2087FN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PT TYPEDEF STRONG GLOBAL.
          05 PP USAGE POINTER.
          05 PX PIC X(2).
       01 WS-REC.
          05 F1 PIC 9.
          05 F2 PIC X(3).
       01 MD1 PIC 9 VALUE 1.
       01 PREC TYPE PT.
       01 TARGET PIC X(4) VALUE "TGT!".
       01 R PIC X(4).
       01 PTR USAGE POINTER.
       PROCEDURE DIVISION.
           CALL "P2087RC" USING BY CONTENT MD1
           MOVE "2ABC" TO WS-REC
           CALL "P2087OM" USING WS-REC OMITTED WS-REC
           DISPLAY "OM AFTER " WS-REC
           MOVE "4ABC" TO WS-REC
           MOVE FUNCTION P2087FN (WS-REC WS-REC) TO R
           DISPLAY "FN RESULT " R " AFTER " WS-REC
           SET PTR TO ADDRESS OF WS-REC
           CALL "P2087AD" USING WS-REC BY CONTENT PTR
           SET PP OF PREC TO ADDRESS OF TARGET
           MOVE "PX" TO PX OF PREC
           CALL "P2087PT" AS NESTED USING PREC
           DISPLAY "PT AFTER " PX OF PREC
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087PT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Q PIC X(4) BASED.
       LINKAGE SECTION.
       01 LP TYPE PT.
       PROCEDURE DIVISION USING LP.
           SET ADDRESS OF Q TO PP OF LP
           DISPLAY "PT POINTER LEAF ADDRESSES " Q
           MOVE "QQ" TO PX OF LP
           GOBACK.
       END PROGRAM P2087PT.
       END PROGRAM P2087M02.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087RC RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MREC.
          05 M1 PIC 9 VALUE 1.
          05 M2 PIC X(3) VALUE "ABC".
       LINKAGE SECTION.
       01 MODE-ARG PIC 9.
       PROCEDURE DIVISION USING MODE-ARG.
           IF MODE-ARG = 1
              DISPLAY "RC INITIAL " MREC
              CALL "P2087CB" USING MREC
              DISPLAY "RC AFTER " MREC
           ELSE
              DISPLAY "RC CALLBACK SEES " MREC
           END-IF
           GOBACK.
       END PROGRAM P2087RC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087CB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MD2 PIC 9 VALUE 2.
       LINKAGE SECTION.
       01 LK.
          05 L1 PIC 9.
          05 L2 PIC X(3).
       PROCEDURE DIVISION USING LK.
           MOVE 7 TO L1
           MOVE "DEF" TO L2
           CALL "P2087RC" USING BY CONTENT MD2
           GOBACK.
       END PROGRAM P2087CB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087OM.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-1.
          05 H1 PIC 9.
          05 H2 PIC X(3).
       01 LK-2.
          05 H3 PIC X(4).
       01 LK-3.
          05 H4 PIC X(4).
       PROCEDURE DIVISION USING LK-1 LK-2 LK-3.
           IF LK-2 IS OMITTED
              DISPLAY "OM LK-2 OMITTED"
           END-IF
           MOVE 6 TO H1
           DISPLAY "OM LK-3 " LK-3
           GOBACK.
       END PROGRAM P2087OM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087AD.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LY.
          05 Y1 PIC X(4).
       01 LPT USAGE POINTER.
       PROCEDURE DIVISION USING LY LPT.
           IF ADDRESS OF LY = LPT
              DISPLAY "AD SAME ADDRESS"
           ELSE
              DISPLAY "AD DIFFERENT ADDRESS"
           END-IF
           GOBACK.
       END PROGRAM P2087AD.
