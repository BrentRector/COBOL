      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1260 - ISO 13.18.38.3 SR10: an OCCURS clause may be
      *>   subordinate to another "as long as the number of subscripts
      *>   required does not exceed seven"; 8.4.2.3.3 SR3: a maximum of
      *>   seven subscripts. A8 below is the eighth dimension.
      *> cite.py --check 13.18.38.3 "as long as the number of subscripts
      *>   required does not exceed seven" -> OK  13.18.38.3 10)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1260C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          02 A1 OCCURS 2.
           03 A2 OCCURS 2.
            04 A3 OCCURS 2.
             05 A4 OCCURS 2.
              06 A5 OCCURS 2.
               07 A6 OCCURS 2.
                08 A7 OCCURS 2.
                 09 A8 PIC X OCCURS 2.
       PROCEDURE DIVISION.
           STOP RUN.
