      *> reject-at: 2002 2014 2023
      *> kb/Work PB1300 -- a TYPE clause composes the template's BASED
      *> clause into its subject ("as though the data description
      *> identified by type-name-1 had been coded in place"; BASED is
      *> not in the exclusion list), so a level-05 subject becomes a
      *> level-05 BASED entry, which §13.16.3 SR16 forbids.
      *> cite.py --check 13.18.57.4 "excluding the level-number, name,
      *>   alignment, and the GLOBAL, SELECT WHEN, and TYPEDEF clauses
      *>   specified for type-name-1" -> OK §13.18.57.4 1)
      *> cite.py --check 13.16.3 "The level number of such data
      *>   description entries shall be 1 or 77" -> OK §13.16.3 16)
      *> The level-01 twin (01 X TYPE T) is legal and based: see
      *> 2002/w66g_pb1300_type_same_as_composition. Expected:
      *> COBOLNET2510 (composed-based-placement) on X.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W66GNB1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T TYPEDEF BASED PIC X(5).
       01  G.
           05  X TYPE T.
       PROCEDURE DIVISION.
           DISPLAY "G=[" G "]".
           STOP RUN.
