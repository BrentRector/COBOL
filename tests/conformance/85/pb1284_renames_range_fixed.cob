      *> kb/Work PB1284 — ISO/IEC 1989:2023 §13.18.45.3 SR8 bars an occurs-depending table WITHIN a RENAMES range
      *> ("None of the items within the range, including data-name-2 and data-name-3, if specified, shall be ...
      *> a variable-length data item, or an occurs-depending table"); the range is §13.18.45.4 GR2's, "all
      *> elementary items starting with data-name-2 ... and concluding with data-name-3". The record below ENDS in
      *> an occurs-depending table, and every range here stops before it, so each
      *> alias is legal and is the storage window it names:
      *>   X1 = A THRU B            = "AB" + "CDE"          -> ABCDE
      *>   X2 = G (no THRU, GR1)    = G's own description    -> CDE (B is G's only subordinate)
      *>   X3 = A THRU G            = the same window as X1  -> ABCDE
      *> (No edition change to SR8 is recorded in docs/VERSION_CHANGE_REFERENCE.md, so this holds at every
      *> edition.) Hand-derived stdout:
      *>   [ABCDE][CDE][ABCDE]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1284RF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 2.
       01 R.
          05 A PIC X(2) VALUE "AB".
          05 G.
             10 B PIC X(3) VALUE "CDE".
          05 T PIC X OCCURS 1 TO 3 DEPENDING ON N.
       66 X1 RENAMES A THRU B.
       66 X2 RENAMES G.
       66 X3 RENAMES A THRU G.
       PROCEDURE DIVISION.
           DISPLAY "[" X1 "][" X2 "][" X3 "]".
           STOP RUN.
