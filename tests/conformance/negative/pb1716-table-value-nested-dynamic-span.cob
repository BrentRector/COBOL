      *> reject-at: 85 2002 2014 2023
      *> ISO 13.18.63.3 SR23: "If the TO phrase is specified and an OCCURS clause with a DYNAMIC phrase
      *> but no TO phrase is specified in the same entry or in any superordinate entry, the values of
      *> subscript-1 and subscript-2 corresponding to all levels higher than that of the OCCURS clause,
      *> if applicable, shall be equal".
      *> The rule binds EVERY such OCCURS clause.  Here both are unbounded: G's has no level above it,
      *> but T's has G's, and FROM (1 1) TO (2 1) differs there - 13.18.63.4 GR12's odometer would
      *> have to carry OUT of T's dimension, which has no ceiling.  kb/Work PB1716: the screen tested
      *> only the OUTERMOST unbounded dimension, so this program compiled clean after a 23-second fill
      *> that ran to a 64,000,000-element cap.  COBOLNET1946 is reported at every edition; below 2014
      *> the OCCURS DYNAMIC introduction gate is reported ALONGSIDE it, not instead of it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1716N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
           03 G OCCURS DYNAMIC CAPACITY IN C0.
               05 T PIC X OCCURS DYNAMIC CAPACITY IN C1
                   VALUE "A" FROM (1 1) TO (2 1).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY T(1 1)
           STOP RUN.
