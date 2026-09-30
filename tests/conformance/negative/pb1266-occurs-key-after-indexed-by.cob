      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1266 - ISO 13.18.38.2 writes the OCCURS clause's KEY
      *>   phrases BEFORE INDEXED BY, so `INDEXED BY IX ASCENDING KEY T`
      *>   is out of order and rejected. The diagnostic used to claim the
      *>   KEY clause "is not yet supported" and that the table had been
      *>   "created without sort key" - false on both counts; it now says
      *>   the KEY phrase is out of place (COBOL0100).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1266B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T PIC 9 OCCURS 3 INDEXED BY IX ASCENDING KEY T.
       PROCEDURE DIVISION.
           STOP RUN.
