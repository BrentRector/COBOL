      *> reject-at: 2002 2014 2023
      *> kb/Work PB1173 — A TABLE SORT KEY IS NOT SUBSCRIPTED.
      *>   cite.py --check 14.9.40.3 "Key data names shall not be subscripted." -> OK §14.9.40.3 14) b)
      *> `SORT TE ASCENDING KEY TK(1)` used to sort exactly as `KEY TK` — the written subscript was dropped without a
      *> word (DataBinder.KeyReference kept the qualifiers only). It is refused by the one data-name-n shape screen
      *> (COBOLNET2024), which says the key is written with a subscript.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1173TS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 TE OCCURS 3.
             10 TK PIC 9.
             10 TV PIC X.
       PROCEDURE DIVISION.
       MAIN.
           SORT TE ASCENDING KEY TK(1)
           STOP RUN.
