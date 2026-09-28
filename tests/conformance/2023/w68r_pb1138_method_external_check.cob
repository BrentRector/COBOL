      *> kb/Work PB1138 - a METHOD activation checks external items (ISO 14.9.23.4
      *> GR7 d): 14.8.4.3 -> 13.18.22 GR6, EC-EXTERNAL-FORMAT-CONFLICT). The program
      *> describes EXX as 8 bytes and EXY as 3; class W68RC1's object data describes EXX
      *> as 4 bytes and its factory data EXY as 2. Checking is enabled in both the
      *> activating statement and the activated method (14.8.4.1), so each invocation
      *> of a W68RC1 method - typed, through a universal reference, and a factory
      *> method - "is not successful": the method never runs, the condition reaches the
      *> declarative, and RESUME AT NEXT STATEMENT continues. Class W68RC2 has the same
      *> nonconforming EXX, but EC-EXTERNAL-FORMAT-CONFLICT is turned OFF before its
      *> method, so checking is not enabled in the activated method and it runs.
      >>TURN EC-EXTERNAL-FORMAT-CONFLICT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W68RP2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS W68RC1
           CLASS W68RC2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O1 USAGE OBJECT REFERENCE W68RC1.
       01 O2 USAGE OBJECT REFERENCE W68RC2.
       01 U USAGE OBJECT REFERENCE.
       01 EXX PIC X(8) EXTERNAL.
       01 EXY PIC X(3) EXTERNAL.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-EXTERNAL-FORMAT-CONFLICT.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE "ABCDEFGH" TO EXX.
           MOVE "XYZ" TO EXY.
           INVOKE W68RC1 "NEW" RETURNING O1.
           INVOKE W68RC2 "NEW" RETURNING O2.
           DISPLAY "TYPED".
           INVOKE O1 "PEEK".
           DISPLAY "UNIVERSAL".
           SET U TO O1.
           INVOKE U "PEEK".
           DISPLAY "FACTORY".
           INVOKE W68RC1 "FPEEK".
           DISPLAY "NOT-IN-METHOD".
           INVOKE O2 "PEEK".
           DISPLAY "AFTER".
           STOP RUN.
       END PROGRAM W68RP2.
       IDENTIFICATION DIVISION.
       CLASS-ID. W68RC1 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EXY PIC X(2) EXTERNAL.
       PROCEDURE DIVISION.
       METHOD-ID. FPEEK.
       PROCEDURE DIVISION.
       FP.
           DISPLAY "C1 FACTORY METHOD " EXY.
       END METHOD FPEEK.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EXX PIC X(4) EXTERNAL.
       PROCEDURE DIVISION.
       METHOD-ID. PEEK.
       PROCEDURE DIVISION.
       P.
           DISPLAY "C1 METHOD " EXX.
       END METHOD PEEK.
       END OBJECT.
       END CLASS W68RC1.
      >>TURN EC-EXTERNAL-FORMAT-CONFLICT CHECKING OFF
       IDENTIFICATION DIVISION.
       CLASS-ID. W68RC2 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EXX PIC X(4) EXTERNAL.
       PROCEDURE DIVISION.
       METHOD-ID. PEEK.
       PROCEDURE DIVISION.
       P.
           DISPLAY "C2 METHOD " EXX.
       END METHOD PEEK.
       END OBJECT.
       END CLASS W68RC2.
