      *> reject-at: 85 2002 2014 2023
      *> ISO 13.18.63.3 SR22: "A VALUE clause without the TO phrase shall not be specified in the same
      *> entry as an OCCURS clause with a DYNAMIC phrase but no TO phrase, OR IN ANY ENTRY SUBORDINATE TO
      *> SUCH AN OCCURS CLAUSE."  X is subordinate to G, whose OCCURS DYNAMIC specifies no TO (expected)
      *> capacity, and X's VALUE has no TO phrase - so 13.18.63.4 GR14's implied subscript-2 has no
      *> number to take.  The SAME-ENTRY arm was implemented; this one had no site and its population
      *> exited through the landable-scope stage instead (kb/Work PB505).
      *> MEASURED AT ALL FOUR EDITIONS: COBOLNET1588 is reported at every one.  SR22 carries no version
      *> proviso, so it does not stop applying because OCCURS DYNAMIC is a COBOL-2014 construct - below
      *> 2014 the construct's own introduction gate (COBOLNET0900, and the format-2 VALUE's at 85) is
      *> reported ALONGSIDE the syntax rule, not instead of it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB505N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GG.
           05 G OCCURS DYNAMIC CAPACITY IN C1.
               10 X PIC X(2) VALUE "AB" FROM (1).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY X(1)
           STOP RUN.
