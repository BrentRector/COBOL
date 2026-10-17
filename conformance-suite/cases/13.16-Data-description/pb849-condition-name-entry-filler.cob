*>--- conformance-case
*> id: pb849-condition-name-entry-filler
*> rules: [SR-13.16.3-24, FMT-13.16.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.16.2 formats 3 and 4 print the level-88 entries as
      *>     88 condition-name-1 value-clause .
      *>     88 [ condition-name-2 ] value-clause .
      *> Neither carries an entry-name clause, so FILLER (13.18.20.2
      *> format 3, the filler format of the entry-name clause, which only
      *> format 1 carries) is not a name either format admits; 13.16.3
      *> SR24 - "Format 3 or 4 is used for each condition-name". Edition-
      *> independent. Before kb/Work PB849 this compiled CLEAN and the
      *> entry evaporated with no diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB849NEG88F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X VALUE "A".
          88 FILLER VALUE "A".
       PROCEDURE DIVISION.
           DISPLAY A
           STOP RUN.
