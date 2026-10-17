      *>--- conformance-case
      *> id: nested_special_names_inheritance
      *> rules: [GR-12.3.4-1, SR-14.9.39.3-5, AR-15.67.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB60 (AR-15.67.3-5), the edition-invariant half at COBOL-85: a container's CONFIGURATION SECTION
      *> applies to its contained programs (12.3.4 GR1; 12.3.3 SR1 - the containee cannot restate it).
      *> DP: the inherited DECIMAL-POINT IS COMMA - a comma numeric literal (12.3.7 GR14a) and a comma-decimal
      *>     PICTURE (GR14b) in the contained program: 12,5 edited as "12,50".
      *> CUR: the inherited CURRENCY SIGN "#" in the contained program's PICTURE: #1.234,50.
      *> CLASS / PCS / SW: the inherited CLASS, PROGRAM COLLATING SEQUENCE and switch condition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CFG85OUTER.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XX
           PROGRAM COLLATING SEQUENCE IS REV.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1 ON STATUS IS SW1-ON OFF STATUS IS SW1-OFF
           ALPHABET REV IS "B" "A"
           CLASS HEXDIG IS "0" THRU "9" "A" THRU "F"
           CURRENCY SIGN IS "#"
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-DUMMY PIC X.
       PROCEDURE DIVISION.
           CALL "CFG85INNER".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CFG85INNER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E    PIC Z9,99.
       01 ED   PIC ##.##9,99.
       01 X    PIC X(4) VALUE "1A9F".
       PROCEDURE DIVISION.
           MOVE 12,5 TO E.
           DISPLAY "DP=" E.
           MOVE 1234,5 TO ED.
           DISPLAY "CUR=" ED.
           IF X IS HEXDIG
               DISPLAY "CLASS=YES"
           ELSE
               DISPLAY "CLASS=NO"
           END-IF.
           IF "A" > "B"
               DISPLAY "PCS=REV"
           ELSE
               DISPLAY "PCS=NATIVE"
           END-IF.
           SET SW1 TO ON.
           IF SW1-ON
               DISPLAY "SW=ON"
           ELSE
               DISPLAY "SW=OFF"
           END-IF.
           EXIT PROGRAM.
       END PROGRAM CFG85INNER.
       END PROGRAM CFG85OUTER.
