      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB3287 - ISO 13.7.3 SR4: "a data item defined in the linkage section of a source element may be
      *> referenced within the procedure division of that source element if, and only if, it satisfies one of the
      *> following conditions".  L-E sits in a LINKAGE record that is no USING operand, no redefinition of one and not
      *> BASED, so naming it as the SEARCH table (14.9.37.3 SR2: identifier-1 is the data item whose OCCURS clause has the INDEXED phrase) is a reference the
      *> procedure division may not make - COBOLNET2746, the verdict `MOVE L-E (1) TO WS-X` already draws.  The
      *> table operand of SEARCH / SORT was looked up by FindItem alone and never asked the screen, so this
      *> compiled clean (the WHEN condition below names no linkage item, so nothing else refuses it).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3287N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-FOUND PIC X VALUE "N".
       LINKAGE SECTION.
       01  L-TBL.
           05  L-E PIC 9 OCCURS 3 INDEXED BY L-X.
       PROCEDURE DIVISION.
       MAIN.
           SEARCH L-E
               AT END CONTINUE
               WHEN WS-FOUND = "Y" CONTINUE
           END-SEARCH.
           STOP RUN.
