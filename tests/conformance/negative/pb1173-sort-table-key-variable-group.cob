      *> reject-at: 2014 2023
      *> kb/Work PB1173 — A TABLE SORT KEY THAT IS A VARIABLE-LENGTH GROUP IS REFUSED BY SR14 d).
      *>   cite.py --check 14.9.40.3 "A key data item shall not reference a variable-length group or an
      *>     occurs-depending group item." -> OK §14.9.40.3 14)
      *> VG holds only a PIC X DYNAMIC LENGTH item (COBOL-2014), which makes it a variable-length group
      *> (§8.5.1.12.1). The Format-2 sort used to compile this clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1173TV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 VE OCCURS 3.
             10 VG.
                15 VD PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN.
           SORT VE ASCENDING KEY VG
           STOP RUN.
