      *> kb/Work PB1175 — THE FORMAT-2 TABLE SORT OF A TABLE THAT HAS NO ARRAY OF ITS OWN: a table inside a REDEFINES
      *> view, a record area shared by several 01s, a BASED record.
      *>
      *> THE RULES, --check validated:
      *>   cite.py --check 14.9.40.4 "The SORT statement sorts the table referenced by data-name-2 and presents the
      *>     sorted table in data-name-2" -> OK §14.9.40.4 18)
      *>   cite.py --check 14.9.40.4 "The sorted table elements of the table referenced by data-name-2 are placed in
      *>     the table referenced by data-name-2" -> OK §14.9.40.4 24)
      *>   cite.py --check 13.18.44.4 "Storage association for the subject of the entry starts at the first bit of the
      *>     data item referenced by data-name-2" -> OK §13.18.44.4 1)
      *> §14.9.40.3 SR13 asks only that data-name-2 "have an OCCURS clause"; no rule excepts a table whose storage is
      *> shared with another description, and GR18/GR24 sort the table and present it in data-name-2, so the sorted
      *> elements ARE the storage the other views read. A compiler that cannot sort such a table must refuse the
      *> source; one that accepts it and aborts at run time (the pre-PB1175 build: COBOLNET1756, then
      *> NotImplementedCobolFeatureException) has rejected legal source.
      *> The Format-2 SORT is a COBOL-2002 introduction (negative/w70c-pb1174-table-sort-below-2002 pins its refusal at
      *> 85); the shared-storage form takes the same edition.
      *>
      *> WHY EACH LEG CAN FAIL (expected values derived from the rules, not copied from a run):
      *>   1  a REDEFINES view D PIC 9 OCCURS 4 over RAW "3142", ASCENDING     -> RAW reads "1234" through the OTHER view
      *>   2  group elements (NUM PIC 99, LTR PIC X) over "03C01A02B04D":
      *>        ASCENDING KEY NUM -> 01A02B03C04D   (whole elements move, not the keys alone)
      *>        DESCENDING KEY LTR -> 04D03C02B01A
      *>   3  DUPLICATES IN ORDER (GR3 c): "02X01Y02Z01W" ASCENDING NUM -> 01Y01W02X02Z   (equal keys keep their order)
      *>   4  a COMP-3 key table PV PIC S9(3) COMP-3 OCCURS 3 holding 3, -1, 2:
      *>        ASCENDING -> -001 +002 +003 (algebraic value, GR19 -> §8.8.4.2.4 — never the packed bytes' order)
      *>        DESCENDING -> +003 +002 -001
      *>   5  a table nested in another table inside the view: ROW OCCURS 4 / E PIC X OCCURS 3 over "CBAFEDIHGLKJ":
      *>        SORT E(3) ASCENDING touches ROW 3 only -> CBAFEDGHILKJ
      *>        SORT E(I + 1, ALL) DESCENDING with I = 2 -> ROW 3 again -> CBAFEDIHGLKJ
      *>   6  a record area shared by two 01s of an FD: B's table D2 over A's "3142"  -> A reads "1234"
      *>   7  a BASED record's table BE over BUF "7316", DESCENDING                  -> BUF reads "7631"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1175TS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb1175ts.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 A PIC X(4).
       01 B.
          05 D2 PIC 9 OCCURS 4.
       WORKING-STORAGE SECTION.
       01 RAW1 PIC X(4).
       01 V1 REDEFINES RAW1.
          05 D PIC 9 OCCURS 4.
       01 RAW2 PIC X(12).
       01 V2 REDEFINES RAW2.
          05 ENT OCCURS 4.
             10 NUM PIC 99.
             10 LTR PIC X.
       01 RAW3 PIC X(6).
       01 V3 REDEFINES RAW3.
          05 PV PIC S9(3) COMP-3 OCCURS 3.
       01 ED1 PIC +999.
       01 ED2 PIC +999.
       01 ED3 PIC +999.
       01 RAW5 PIC X(12).
       01 V5 REDEFINES RAW5.
          05 ROW OCCURS 4.
             10 E PIC X OCCURS 3.
       01 I PIC 9 VALUE 2.
       01 BUF PIC X(4) VALUE "7316".
       01 BR BASED.
          05 BE PIC 9 OCCURS 4.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "3142" TO RAW1
           SORT D ASCENDING
           DISPLAY "1 " RAW1
           MOVE "03C01A02B04D" TO RAW2
           SORT ENT ASCENDING KEY NUM
           DISPLAY "2a " RAW2
           SORT ENT DESCENDING KEY LTR
           DISPLAY "2b " RAW2
           MOVE "02X01Y02Z01W" TO RAW2
           SORT ENT ASCENDING KEY NUM WITH DUPLICATES IN ORDER
           DISPLAY "3 " RAW2
           MOVE 3 TO PV(1)
           MOVE -1 TO PV(2)
           MOVE 2 TO PV(3)
           SORT PV ASCENDING
           MOVE PV(1) TO ED1
           MOVE PV(2) TO ED2
           MOVE PV(3) TO ED3
           DISPLAY "4a " ED1 ED2 ED3
           SORT PV DESCENDING
           MOVE PV(1) TO ED1
           MOVE PV(2) TO ED2
           MOVE PV(3) TO ED3
           DISPLAY "4b " ED1 ED2 ED3
           MOVE "CBAFEDIHGLKJ" TO RAW5
           SORT E(3) ASCENDING
           DISPLAY "5a " RAW5
           SORT E(I + 1, ALL) DESCENDING
           DISPLAY "5b " RAW5
           MOVE "3142" TO A
           SORT D2 ASCENDING
           DISPLAY "6 " A
           SET ADDRESS OF BR TO ADDRESS OF BUF
           SORT BE DESCENDING
           DISPLAY "7 " BUF
           STOP RUN.
