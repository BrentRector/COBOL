      *> reject-at: 2002 2014 2023
      *> kb/Work PB1128 - ISO 8.5.2.1 Table 2 puts a numeric-edited item "(if usage is national)"
      *> in class NATIONAL, so ISO 14.9.22.3 SR4 - "If any of identifier-1, ... literal-1 ...
      *> references an elementary data item or literal of class boolean or national, then all
      *> shall reference a data item or literal of class boolean or national, respectively" -
      *> refuses the alphanumeric literal-1 " " beside it. The class used to be read off the
      *> category alone, which answered "alphanumeric". USAGE NATIONAL arrives in 2002.
      *> Expected: COBOLNET2306 (character-operand-class-mix).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB1128NATNE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NE PIC ZZ9 USAGE NATIONAL VALUE N"  5".
       01 C  PIC 99 VALUE 0.
       PROCEDURE DIVISION.
           INSPECT NE TALLYING C FOR ALL " "
           DISPLAY C
           STOP RUN.
