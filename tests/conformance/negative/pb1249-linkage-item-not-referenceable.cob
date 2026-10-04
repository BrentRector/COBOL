      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1249 -- ISO 13.7.3 SR4 (cite.py --check 13.7.3 "may be referenced within the
      *> procedure division of that source element if, and only if, it satisfies one of the
      *> following conditions" -> OK 13.7.3 4)). L-Z is a linkage record that is not an operand
      *> of the USING phrase, not subordinate to one, not a redefinition or renaming of one and
      *> not BASED, so MOVE 5 TO L-Z references an item the procedure division may not reference.
      *> The compiler used to give L-Z callee-local storage and run the program: COBOLNET2746.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1249N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A       PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB1249N1C" USING A.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1249N1C.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-A     PIC 9(4).
       01  L-Z     PIC 9(4).
       PROCEDURE DIVISION USING L-A.
       SUB-P.
           MOVE 5 TO L-Z.
           DISPLAY L-Z.
           EXIT PROGRAM.
       END PROGRAM PB1249N1C.
       END PROGRAM PB1249N1.
