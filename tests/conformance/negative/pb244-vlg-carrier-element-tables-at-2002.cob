      *> reject-at: 85 2002
      *> kb/Work PB244 - the group MOVE, comparison and CALL of a variable-length group whose
      *> OCCURS DEPENDING table holds variable-length ELEMENTS are COBOL-2014 programs: the
      *> DYNAMIC LENGTH clause that makes each occurrence a variable-length group (ISO
      *> 8.5.1.12.1, 13.18.19) is a 2014 addition, so editions 85 and 2002 reject the
      *> description with the edition-band diagnostic COBOLNET0900 BEFORE the statements'
      *> compatibility rule (14.9.25.3 SR9, 8.5.1.12) is ever in question.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244NCA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 VALUE 2.
       01 G1.
          05 TE OCCURS 1 TO 3 DEPENDING ON K.
             10 DD PIC X DYNAMIC LENGTH LIMIT 5.
             10 FX PIC X.
       01 G2.
          05 TE2 OCCURS 1 TO 3 DEPENDING ON K.
             10 DD2 PIC X DYNAMIC LENGTH LIMIT 5.
             10 FX2 PIC X.
       PROCEDURE DIVISION.
           MOVE G1 TO G2
           IF G1 = G2 DISPLAY "EQ" END-IF
           STOP RUN.
