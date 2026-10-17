      *>--- conformance-case
      *> id: l1c04_index_category_implicit
      *> rules: [GR-8.5.2.8-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.5.2.8 GR1 — an item IMPLICITLY usage index (inherited
      *>   from its group) is an index data item, like an explicit one.
      *> cite.py --check 8.5.2.8 "An elementary data item explicitly or
      *>   implicitly described as usage index" -> OK §8.5.2.8 1)
      *> cite.py --check 13.18.60.4 "If the USAGE clause is specified or
      *>   implied at a group level, it applies only to each elementary
      *>   item in the group" -> OK §13.18.60.4 1)
      *> cite.py --check 13.18.60.4 "The USAGE INDEX clause specifies
      *>   that a data item is an index data item" -> OK §13.18.60.4 10)
      *>   (same GR: its value "shall correspond to an occurrence
      *>   number of a table element"; the representation is the
      *>   implementor's, so only round trips and equality are shown.)
      *> I1 and I2 carry no USAGE of their own; G USAGE INDEX gives it
      *> to them implicitly. IX is the explicit form. TE = "ABCDE".
      *>   "1 TE=D" SET I1 TO TX (TX=4); SET TX TO I1 -> occurrence 4.
      *>   "2 TE=A" SET I2 TO TX (TX=1); SET TX TO I2 -> occurrence 1.
      *>   "3 EQ"   SET I2 TO I1 (index data item to index data item);
      *>            relation I1 = I2 is true.
      *>   "4 TE=D" SET TX TO I2 -> I2 now holds occurrence 4.
      *>   "5 TE=B" explicit IX: SET IX TO TX (TX=2); SET TX TO IX.
      *> That MOVE to I1 is refused (class index) is the companion
      *> negative/l1c04-implicit-index-move-target.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C04J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T VALUE "ABCDE".
          05 TE PIC X OCCURS 5 INDEXED BY TX.
       01 G USAGE INDEX.
          05 I1.
          05 I2.
       01 IX USAGE INDEX.
       PROCEDURE DIVISION.
       MAIN.
           SET TX TO 4.
           SET I1 TO TX.
           SET TX TO 5.
           SET TX TO I1.
           DISPLAY "1 TE=" TE (TX).
           SET TX TO 1.
           SET I2 TO TX.
           SET TX TO 5.
           SET TX TO I2.
           DISPLAY "2 TE=" TE (TX).
           SET I2 TO I1.
           IF I1 = I2
               DISPLAY "3 EQ"
           ELSE
               DISPLAY "3 NE"
           END-IF.
           SET TX TO I2.
           DISPLAY "4 TE=" TE (TX).
           SET TX TO 2.
           SET IX TO TX.
           SET TX TO 5.
           SET TX TO IX.
           DISPLAY "5 TE=" TE (TX).
           STOP RUN.
