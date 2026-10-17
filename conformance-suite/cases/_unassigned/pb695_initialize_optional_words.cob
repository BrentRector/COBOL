*>--- conformance-case
*> id: pb695_initialize_optional_words
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> !! THE INITIALIZE STATEMENT'S OPTIONAL WORDS, OMITTED (kb/Work PB695 family 2).
      *> ISO 14.9.20.2, read off the printed page (PDF p667 / folio 637). The statement's underlines fall
      *> on INITIALIZE, FILLER, ALL, VALUE, REPLACING, BY, DEFAULT and the thirteen category names. WITH,
      *> the two THENs, DATA and BOTH occurrences of TO are printed plain, so 8.3.2.4.3 makes every
      *> spelling here conforming source. COBOL.NET required both TOs and answered COBOL0001.
      *>   . `INITIALIZE G1 WITH FILLER ALL TO VALUE`  - fully written
      *>   . `INITIALIZE G1 FILLER ALL VALUE`          - WITH and TO omitted  (this program)
      *>   . `INITIALIZE N1 THEN TO DEFAULT` / `THEN DEFAULT` / `TO DEFAULT` / `DEFAULT`
      *> The bare `INITIALIZE N1 DEFAULT` is why initializeOperandList carries the reservedHere("DEFAULT")
      *> guard: DEFAULT rides cobolWord, so the greedy `{identifier-1}...` loop would otherwise take it as
      *> a second operand and the statement would die on COBOLNET1639. 8.9 reserves DEFAULT from 2002, so
      *> at this edition it can never be a user-defined word (8.3.2.1); at 85 the loop still absorbs it,
      *> which is the correct 85 reading because the DEFAULT phrase does not exist there.
      *> Expected values, derived from 14.9.20.4. GR5 c) 1. makes an item a receiving-operand under the
      *> VALUE phrase when it has a data-item VALUE clause, and GR6 a) 3. makes its sending-operand that
      *> clause's literal; GR5 a) 2. excludes FILLER items UNLESS the FILLER phrase is written, which it
      *> is here - so G1 becomes "ZZ" + "AB" = "ZZAB". GR5 c) 3. makes every possible receiving-operand a
      *> receiving-operand under the DEFAULT phrase, and GR6 c)'s table fixes the sending-operand by
      *> CATEGORY: Numeric -> figurative constant ZEROES, Alphanumeric -> figurative constant SPACES - so
      *> N1 is 00 and X1 is three spaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695INIOW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  FILLER      PIC X(2) VALUE "ZZ".
           05  G1-A        PIC X(2) VALUE "AB".
       01  N1              PIC 9(2).
       01  X1              PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "QQQQ" TO G1
           MOVE 77 TO N1
           MOVE "PQR" TO X1
           INITIALIZE G1 FILLER ALL VALUE
           DISPLAY "G1=" G1
           INITIALIZE N1 DEFAULT
           DISPLAY "N1=" N1
           INITIALIZE X1 THEN DEFAULT
           DISPLAY "X1=[" X1 "]"
           DISPLAY "DONE"
           STOP RUN.
