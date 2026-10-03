      *> reject-at: 2002 2014 2023
      *> kb/Work PB1173 — A TABLE SORT KEY OF CLASS BOOLEAN IS REFUSED BY SR14 c).
      *>   cite.py --check 14.9.40.3 "Key data items shall not be of class boolean, object, or pointer." ->
      *>     OK §14.9.40.3 14)
      *> TB is PIC 1 USAGE BIT inside the table element TE. The Format-2 sort asked nothing of its keys' classes and
      *> compiled this clean. (USAGE BIT is a COBOL-2002 construct, as is the table format itself.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1173TB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 TE OCCURS 3.
             10 TK PIC 9.
             10 TB PIC 1 USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           SORT TE ASCENDING KEY TB
           STOP RUN.
