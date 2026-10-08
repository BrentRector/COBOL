      *> reject-at: 2002 2014 2023
      *> kb/Work PB1430 (the 8.7.4 rule's 8.7.3 twin) - ISO 8.7.3: "The
      *> concatenation operator is the COBOL character '&', which shall be
      *> immediately preceded and followed by a separator space." Both
      *> operands are figurative constants (8.8.3.2 SR1 admits them), so
      *> no literal delimiter touches the operator and no 8.3.5 rule can
      *> see SPACE&SPACE; it used to compile and run. COBOLNET2993.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1430N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W  PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           MOVE SPACE&SPACE TO W.
           DISPLAY "[" W "]".
           STOP RUN.
       END PROGRAM PB1430N3.
