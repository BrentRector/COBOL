      *> kb/Work PB1055 — THE SUBJECT OF A FORMAT-2 TABLE SORT THAT LIES INSIDE ANOTHER TABLE.
      *>
      *> THE RULES, --check validated:
      *>   cite.py --check 14.9.40.3 "Data-name-2 shall have an OCCURS clause in its data description entry.
      *>     Subscripting shall be specified in accordance with 8.4.2.3, Subscripts." -> OK §14.9.40.3 13)
      *>   cite.py --check 8.4.2.3.3 "As the subject of a SORT statement that references a table where the rightmost
      *>     subscript is not the word ALL. If the rightmost subscript is the word ALL, the number of subscripts is
      *>     equal to one less than the number of OCCURS clauses in the description of the table element being
      *>     referenced." -> OK §8.4.2.3.3 5) e)
      *>   cite.py --check 8.4.2.3.3 "as the rightmost or only subscript of a table in the table format of a SORT
      *>     statement. This is equivalent to omitting the rightmost or only subscript in this context." -> OK
      *>     §8.4.2.3.3 6)
      *> So the subject names the table to sort by one subscript for each ENCLOSING table, written outermost first and
      *> none for the sorted level, which a rightmost ALL may stand for: SORT E(2) over E inside ROW OCCURS 2 sorts
      *> the E of ROW(2) and nothing else. (kb/Work PB1055: the statement used to bind to a deferral that aborted the
      *> run unit, and the written subscripts were never read.)
      *> The Format-2 SORT is a COBOL-2002 introduction; negative/w70c-pb1174-table-sort-below-2002 pins its refusal
      *> at 85, and negative/pb1055-table-sort-subject-subscripts the subscripts' own rule.
      *>
      *> WHY EACH LEG CAN FAIL (expected values derived from the rules, not copied from a run):
      *>   1  T = "CBAFED" (ROW(1) = CBA, ROW(2) = FED), SORT E(2) ASCENDING       -> CBADEF (ROW 2 only)
      *>   2  SORT E(1, ALL) ASCENDING                                              -> ABCDEF (ROW 1 only; ALL = omitted)
      *>   3  I = 2, SORT E(I) DESCENDING                                           -> ABCFED (the subscript is a data-name)
      *>   4  group elements EN (K PIC 9, V PIC X) in BLK OCCURS 2, "3a1b2c" and "9z8y7x":
      *>        SORT EN(2) ASCENDING KEY K  -> BLK(1) untouched, BLK(2) = 7x8y9z    -> 3a1b2c7x8y9z
      *>   5  three levels P / Q / R holding "DCBAHGFE":
      *>        SORT R(2, 1) ASCENDING     -> P(2) Q(1) "HG" becomes "GH"           -> DCBAGHFE
      *>        SORT R(1, 2, ALL) ASCENDING -> P(1) Q(2) "BA" becomes "AB"          -> DCABGHFE
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1055NS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 ROW OCCURS 2.
             10 E PIC X OCCURS 3.
       01 I PIC 9 VALUE 2.
       01 G.
          05 BLK OCCURS 2.
             10 EN OCCURS 3.
                15 K PIC 9.
                15 V PIC X.
       01 C3.
          05 P OCCURS 2.
             10 Q OCCURS 2.
                15 R PIC X OCCURS 2.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "CBAFED" TO T
           SORT E(2) ASCENDING
           DISPLAY "1 " T
           SORT E(1, ALL) ASCENDING
           DISPLAY "2 " T
           SORT E(I) DESCENDING
           DISPLAY "3 " T
           MOVE "3a1b2c" TO BLK(1)
           MOVE "9z8y7x" TO BLK(2)
           SORT EN(2) ASCENDING KEY K
           DISPLAY "4 " BLK(1) BLK(2)
           MOVE "DCBAHGFE" TO C3
           SORT R(2, 1) ASCENDING
           DISPLAY "5a " C3
           SORT R(1, 2, ALL) ASCENDING
           DISPLAY "5b " C3
           STOP RUN.
