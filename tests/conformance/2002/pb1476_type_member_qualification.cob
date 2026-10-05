      *> kb/Work PB1476 - ISO 8.4.2.2.1 rule 4: qualification is not required
      *>   when 'Any other definition of the name is subordinate to a type
      *>   declaration entry for which the type-name is not referenced in any
      *>   TYPE clause in the source unit'. T1 IS referenced (by R inside T2),
      *>   so X needs qualification - X OF G is unique. U1's member M has ONE
      *>   copy (Z), so 13.18.58.4 GR1 needs no qualification for it ('If there
      *>   is more than one such group, qualification ... is necessary').
      *> Expected: GQ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1476QUA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 TYPEDEF.
          05 X PIC X.
       01 T2 TYPEDEF.
          05 R TYPE T1.
       01 G.
          05 X PIC X VALUE "G".
       01 U1 TYPEDEF.
          05 M PIC X.
       01 Z TYPE U1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "Q" TO M
           DISPLAY X OF G M
           STOP RUN.
