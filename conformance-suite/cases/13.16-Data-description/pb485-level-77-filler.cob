*>--- conformance-case
*> id: pb485-level-77-filler
*> rules: [SR-13.16.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.16.3 SR2: "The data-name format of the entry-name clause
      *> shall be specified if level-number is 77." 13.18.33.4 GR2a says
      *> why -- level 77 identifies a NONCONTIGUOUS working storage, local
      *> storage or linkage item, and an item nothing can name is not one:
      *> it has no group to be a part of and no name to be referenced by,
      *> so it is unreachable storage. FILLER is the filler format of the
      *> entry-name clause, not the data-name format. kb/Work PB485.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB485NB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  V PIC X(3) VALUE "ABC".
       77  FILLER PIC X(3).
       PROCEDURE DIVISION.
           DISPLAY V
           STOP RUN.
