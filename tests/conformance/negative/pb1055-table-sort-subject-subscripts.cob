      *> reject-at: 2002 2014 2023
      *> kb/Work PB1055 — THE SUBJECT OF A TABLE SORT WRITES ONE SUBSCRIPT FOR EACH ENCLOSING TABLE AND NONE FOR ITS OWN.
      *>   cite.py --check 8.4.2.3.3 "Except as defined in Syntax rule 5, when a reference is made to a table element,
      *>     the number of subscripts shall equal the number of OCCURS clauses in the description of the table element
      *>     being referenced" -> OK §8.4.2.3.3 3)
      *>   cite.py --check 8.4.2.3.3 "As the subject of a SORT statement that references a table where the rightmost
      *>     subscript is not the word ALL" -> OK §8.4.2.3.3 5) e)
      *> E lies in ROW OCCURS 2, so `SORT E(2)` and `SORT E(2, ALL)` are the legal spellings. Each statement below is
      *> refused (COBOLNET2097): an OMITTED outer subscript names no table to sort, a surplus one names an element, and a
      *> table that lies in no other table takes none (ROW(1) would be one element of ROW, not the table).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1055NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 ROW OCCURS 2.
             10 E PIC X OCCURS 3.
       PROCEDURE DIVISION.
       MAIN.
           SORT E ASCENDING
           SORT E(1, 2) ASCENDING
           SORT E(1, 2, 3) ASCENDING
           SORT ROW(1) ASCENDING
           STOP RUN.
