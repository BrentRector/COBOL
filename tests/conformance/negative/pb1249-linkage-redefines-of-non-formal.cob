      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1249 -- ISO 13.7.3 SR4 c) (cite.py --check 13.7.3 "It is defined with a REDEFINES
      *> or RENAMES clause, the object of which satisfies one of the above conditions" -> OK 13.7.3
      *> 4)). L-ZR REDEFINES L-Z, and the OBJECT L-Z satisfies none of a)-b) (it is not a USING
      *> operand, nor under one, nor BASED), so the redefinition is not referenceable:
      *> COBOLNET2746. The positive twin (L-A2 REDEFINES the USING operand L-A) is
      *> 85/pb1249_linkage_referenceable_legs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1249N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A       PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB1249N4C" USING A.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1249N4C.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-A     PIC 9(4).
       01  L-Z     PIC X(4).
       01  L-ZR    REDEFINES L-Z PIC 9(4).
       PROCEDURE DIVISION USING L-A.
       SUB-P.
           DISPLAY L-ZR.
           EXIT PROGRAM.
       END PROGRAM PB1249N4C.
       END PROGRAM PB1249N4.
