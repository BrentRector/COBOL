      *> reject-at: 2002 2014 2023
      *> ISO 8.4.2.2.1 rule 4 is about 'the name' - a condition-name and an
      *>   index-name of a type declaration's member are definitions too
      *>   (13.18.58.4 GR1: subordinate entries and condition-name entries
      *>   'are part of the type declaration'). T1 is referenced by R in T2,
      *>   so CA and IX in T1 lapse the exemption and the unqualified
      *>   references below are ambiguous (kb/Work PB1476).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1476NCI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 TYPEDEF.
          05 X PIC X OCCURS 2 INDEXED BY IX.
          05 C PIC X.
             88 CA VALUE "A".
       01 T2 TYPEDEF.
          05 R TYPE T1.
       01 G.
          05 Y PIC X OCCURS 2 INDEXED BY IX.
          05 D PIC X VALUE "A".
             88 CA VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF CA DISPLAY "UNREACHABLE" END-IF
           SET IX TO 1
           STOP RUN.
