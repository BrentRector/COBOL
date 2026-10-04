      *> EXTERNAL type declaration (ISO 13.18.22 SR1/GR2/GR3 + 13.18.58.3 SR3, COBOL-2002; P10 Step 16 -
      *> the former COBOLNET1534 stage LIFTED). EXTERNAL on a level-1 TYPEDEF puts the external attribute on
      *> the RECORDS declared with that type (13.18.22 GR3 - the type declaration itself has no storage,
      *> 13.18.58.4 GR2): each level-1 TYPE reference becomes an EXTERNAL record sharing ONE run-unit
      *> ExternalStore cell by its externalized name (GR1/GR5/GR6). MAIN sets the fields, the called program
      *> (its own identical declarations, same run unit) reads them and writes back - both directions prove
      *> the ONE shared storage.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TYPEDEF-EXT-P10TS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CFG-T TYPEDEF IS EXTERNAL.
          05 CFG-NAME PIC X(5).
          05 CFG-NUM  PIC 9(3).
       01 SHARED-CFG TYPE CFG-T.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "HELLO" TO CFG-NAME OF SHARED-CFG.
           MOVE 123 TO CFG-NUM OF SHARED-CFG.
           CALL "TYPEDEF-EXT-SUB-P10TS".
           DISPLAY "MAIN=[" CFG-NAME OF SHARED-CFG "]["
               CFG-NUM OF SHARED-CFG "]".
           STOP RUN.
       END PROGRAM TYPEDEF-EXT-P10TS.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. TYPEDEF-EXT-SUB-P10TS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CFG-T TYPEDEF IS EXTERNAL.
          05 CFG-NAME PIC X(5).
          05 CFG-NUM  PIC 9(3).
       01 SHARED-CFG TYPE CFG-T.
       PROCEDURE DIVISION.
       SUB-PARA.
           DISPLAY "SUB=[" CFG-NAME OF SHARED-CFG "]["
               CFG-NUM OF SHARED-CFG "]".
           MOVE 456 TO CFG-NUM OF SHARED-CFG.
           EXIT PROGRAM.
