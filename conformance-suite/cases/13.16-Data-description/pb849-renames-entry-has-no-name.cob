*>--- conformance-case
*> id: pb849-renames-entry-has-no-name
*> rules: [FMT-13.16.2, FMT-13.18.45.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.16.2 format 2 (and 13.18.45.2) print the renames entry as
      *>     66 data-name-1 RENAMES data-name-4 [ THRU data-name-5 ] .
      *> data-name-1 is UNBRACKETED and is not an entry-name clause, so
      *> the entry must be named: omitting the name is not the format,
      *> and FILLER (13.18.20.2 format 3, the entry-name clause's filler
      *> format, which only format 1 carries) is not data-name-1 either.
      *> 13.18.33.4 GR2b: level 66 "may be used only as described by the
      *> renames format". Edition-independent. Before kb/Work PB849 both
      *> entries compiled CLEAN and evaporated with no diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB849NEG66.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A PIC X VALUE "A".
          05 B PIC X VALUE "B".
       66 RENAMES A THRU B.
       66 FILLER RENAMES A THRU B.
       PROCEDURE DIVISION.
           DISPLAY G
           STOP RUN.
