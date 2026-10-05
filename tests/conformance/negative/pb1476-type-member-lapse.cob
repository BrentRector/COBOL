      *> reject-at: 2002 2014 2023
      *> ISO 8.4.2.2.1: qualification is required unless '4) Any other
      *>   definition of the name is subordinate to a type declaration entry
      *>   for which the type-name is not referenced in any TYPE clause in the
      *>   source unit'. T1 is referenced by the TYPE clause of R inside T2 -
      *>   a TYPE clause in the source unit although T2 itself is unreferenced
      *>   - so X in T1 lapses the exemption and unqualified X is ambiguous
      *>   (kb/Work PB1476).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1476NLP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 TYPEDEF.
          05 X PIC X.
       01 T2 TYPEDEF.
          05 R TYPE T1.
       01 G.
          05 X PIC X VALUE "G".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY X
           STOP RUN.
