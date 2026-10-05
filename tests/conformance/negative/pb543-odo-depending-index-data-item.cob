*> reject-at: 85 2002 2014 2023
      *> kb/Work PB543 - 13.18.38.3 SR17: data-name-1 of OCCURS ... DEPENDING ON "shall describe an integer". The code
      *> comment already said "an index item is NOT an integer data item" (8.5.2.1 Table 2: class index), but the
      *> test was the pattern Category: Numeric, Scale: 0, which USAGE INDEX satisfies, and 13.18.60.3 SR10 lists no
      *> OCCURS clause among the contexts that may reference an index data item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB543ODO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IZ USAGE INDEX.
       01 T.
          02 E PIC X OCCURS 1 TO 5 DEPENDING ON IZ.
       PROCEDURE DIVISION.
           STOP RUN.
