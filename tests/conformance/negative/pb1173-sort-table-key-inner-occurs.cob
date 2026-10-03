      *> reject-at: 2002 2014 2023
      *> kb/Work PB1173 — A TABLE SORT KEY UNDER AN INNER OCCURS IS REFUSED BY SR14 e).
      *>   cite.py --check 14.9.40.3 "If the data item identified by a key data-name is subordinate to data-name-2, it
      *>     shall not be described with an OCCURS clause, and it shall not be subordinate to an entry that is also
      *>     subordinate to data-name-2 and contains an OCCURS clause" -> OK §14.9.40.3 14) e)
      *> TIN is described with its own OCCURS inside the table element TE, so it cannot be a key of the sort of TE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1173TI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 TE OCCURS 3.
             10 TK PIC 9.
             10 TIN PIC X OCCURS 2.
       PROCEDURE DIVISION.
       MAIN.
           SORT TE ASCENDING KEY TIN
           STOP RUN.
