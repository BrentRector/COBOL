*> reject-at: 85 2002 2014 2023
      *> kb/Work PB2018 - the same rule on the SUBJECT side and with an index-name that is not parenthesised:
      *> `IF K + 1 = 4` has an arithmetic expression for its subject, so K is an operand of the expression and
      *> not an operand of the relation (13.18.38.3 SR7; 8.8.4.2.13). The 85/index_name_r7_windows golden
      *> listed this shape as legal until PB2018 read r7 and 8.8.4.2.13 again.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2018SUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          02 E PIC X OCCURS 5 INDEXED BY K.
       PROCEDURE DIVISION.
           SET K TO 3
           IF K + 1 = 4 DISPLAY "UNDER-REJECT" END-IF
           STOP RUN.
