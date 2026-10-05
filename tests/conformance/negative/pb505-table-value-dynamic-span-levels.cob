      *> reject-at: 85 2002 2014 2023
      *> ISO 13.18.63.3 SR23: "If the TO phrase is specified and an OCCURS clause with a DYNAMIC phrase
      *> but no TO phrase is specified in the same entry or in any superordinate entry, the values of
      *> subscript-1 and subscript-2 corresponding to all levels higher than that of the OCCURS clause,
      *> if applicable, shall be equal".
      *> T's own OCCURS DYNAMIC specifies no TO capacity, so its dimension has no ceiling and
      *> 13.18.63.4 GR12's odometer can never carry OUT of it into G's - which is what FROM (1 1) TO
      *> (2 3) asks it to do.  The rule had NO reachable population before kb/Work PB505 landed: its
      *> antecedent needs more than one subscript position, and every such tuple was refused by the
      *> landable-scope stage (COBOLNET0899), so a green test pinned the STAGE and SR23 looked covered.
      *> MEASURED AT ALL FOUR EDITIONS: COBOLNET1946 is reported at every one.  SR23 carries no version
      *> proviso; below 2014 the OCCURS DYNAMIC introduction gate (COBOLNET0900, and the format-2
      *> VALUE's at 85) is reported ALONGSIDE it, not instead of it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB505N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GG.
           05 G OCCURS 2.
               10 T PIC X OCCURS DYNAMIC CAPACITY IN C1
                   VALUE "A" FROM (1 1) TO (2 3).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY T(1 1)
           STOP RUN.
