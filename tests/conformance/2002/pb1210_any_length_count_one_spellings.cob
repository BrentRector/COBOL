      *> ISO 13.18.2.3 SR1: "The character-string specified in that PICTURE clause shall be one instance
      *> of the picture symbol 'N', 'X', or '1'." 13.18.40.3 SR6: the parenthesized integer "indicates the
      *> number of consecutive occurrences of the symbol", so X(01) and X(001) are ONE instance of X and
      *> satisfy SR1 exactly as X does (kb/Work PB1210: they were refused COBOLNET1542 by a closed list of
      *> six spellings while the DYNAMIC LENGTH twin accepted them). Expected (derived, 13.18.2.4 GR1 /
      *> 14.8.2.3.2 2) d): each formal takes the length of its argument - LEN=03 VAL=XYZ, LEN=08 VAL=WXYZABCD,
      *> then the X(001) formal LEN=03 VAL=XYZ. A refusal, or a one-character window, fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1210MAIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A3 PIC XXX VALUE "XYZ".
       01 A8 PIC X(8) VALUE "WXYZABCD".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB1210S1" USING A3.
           CALL "PB1210S1" USING A8.
           CALL "PB1210S2" USING A3.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1210S1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 99.
       LINKAGE SECTION.
       01 L PIC X(01) ANY LENGTH.
       PROCEDURE DIVISION USING L.
       S1.
           MOVE FUNCTION LENGTH(L) TO N.
           DISPLAY "LEN=" N " VAL=" L.
           EXIT PROGRAM.
       END PROGRAM PB1210S1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1210S2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 99.
       LINKAGE SECTION.
       01 L PIC X(001) ANY LENGTH.
       PROCEDURE DIVISION USING L.
       S2.
           MOVE FUNCTION LENGTH(L) TO N.
           DISPLAY "LEN=" N " VAL=" L.
           EXIT PROGRAM.
       END PROGRAM PB1210S2.
       END PROGRAM PB1210MAIN.
