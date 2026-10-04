      *> kb/Work PB1118 - ISO 14.6.13.2 rule 5: "When the internal format of a dynamic-length
      *> elementary item is not correctly formed or does not agree with the corresponding DYNAMIC
      *> LENGTH clause an EC-DATA-INCOMPATIBLE exception condition is set to exist." The caller's D
      *> (LIMIT 10) holds 8 characters; the callee describes the same storage (14.2.3 GR8, "as if the
      *> formal parameter occupies the same storage area as the argument") as F with LIMIT 4, so the
      *> content does not agree with THAT clause and the sending reference in MOVE F TO W4 sets the
      *> fatal condition: the declarative runs and RESUME AT NEXT STATEMENT leaves W4 unchanged.
      *> A1/A2: 8 characters, checked -> HANDLED, W4 [....].
      *> B:     2 characters agree with LIMIT 4 -> no condition, W4 [AB  ].
      *> C:     the EXTERNAL record E is LIMIT 10 here and LIMIT 4 in P1118B: the same agreement.
      *> D:     P1118C is compiled with checking OFF: the condition does not exist (14.6.13.1.1) and
      *>        the statement completes with the content it found - W4 [ABCD].
       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1118A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC X DYNAMIC LENGTH LIMIT 10.
       01 E EXTERNAL.
          05 EH PIC X(2).
          05 DX PIC X DYNAMIC LENGTH LIMIT 10.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCDEFGH" TO D.
           MOVE "AB" TO DX.
           CALL "P1118B" USING D.
           MOVE "AB" TO D.
           CALL "P1118B" USING D.
           MOVE "ABCDEFGH" TO D.
           MOVE "ABCDEFGHIJ" TO DX.
           CALL "P1118B" USING D.
           MOVE "ABCDEFGH" TO D.
           CALL "P1118C" USING D.
           DISPLAY "MAIN-AFTER".
           STOP RUN.
       END PROGRAM P1118A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1118B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W4 PIC X(4).
       01 E EXTERNAL.
          05 EH PIC X(2).
          05 DX PIC X DYNAMIC LENGTH LIMIT 4.
       LINKAGE SECTION.
       01 F PIC X DYNAMIC LENGTH LIMIT 4.
       PROCEDURE DIVISION USING F.
       DECLARATIVES.
       DI SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-INCOMPATIBLE.
       DI-1.
           DISPLAY "HANDLED " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-B.
           MOVE "...." TO W4.
           MOVE F TO W4.
           DISPLAY "W4 [" W4 "]".
           MOVE "...." TO W4.
           MOVE DX TO W4.
           DISPLAY "EX [" W4 "]".
           GOBACK.
       END PROGRAM P1118B.
       >>TURN EC-DATA-INCOMPATIBLE CHECKING OFF
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1118C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W4 PIC X(4).
       LINKAGE SECTION.
       01 F PIC X DYNAMIC LENGTH LIMIT 4.
       PROCEDURE DIVISION USING F.
       MAIN-C.
           MOVE "...." TO W4.
           MOVE F TO W4.
           DISPLAY "UNCHECKED W4 [" W4 "]".
           GOBACK.
       END PROGRAM P1118C.
