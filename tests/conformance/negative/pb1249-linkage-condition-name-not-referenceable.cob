      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1249 -- ISO 13.7.3 SR4 e) (cite.py --check 13.7.3 "It is a condition-name or
      *> index-name associated with a data item that satisfies one of the above conditions"
      *> -> OK 13.7.3 4)). L-Z-ON is a condition-name of L-Z, and L-Z satisfies none of a)-d)
      *> (it is not a USING operand, nor under one, nor a redefinition, nor BASED), so the
      *> condition-name is not referenceable either: COBOLNET2746.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1249N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A       PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB1249N2C" USING A.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1249N2C.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-A     PIC 9(4).
       01  L-Z     PIC 9(4).
           88  L-Z-ON VALUE 1.
       PROCEDURE DIVISION USING L-A.
       SUB-P.
           IF L-Z-ON DISPLAY "ON".
           EXIT PROGRAM.
       END PROGRAM PB1249N2C.
       END PROGRAM PB1249N2.
