      *> reject-at: 2002 2014 2023
      *> kb/Work PB1173 — A TABLE SORT KEY MUST BE THE TABLE OR SUBORDINATE TO IT.
      *>   cite.py --check 14.9.40.3 "The data item identified by a key data-name shall be the same as, or subordinate
      *>     to, the data item referenced by data-name-2." -> OK §14.9.40.3 14) a)
      *> OTHER-K is a separate item outside the table TE, so it is not a key of the sort of TE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1173TO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 TE OCCURS 3.
             10 TK PIC 9.
       01 OTHER-K PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           SORT TE ASCENDING KEY OTHER-K
           STOP RUN.
