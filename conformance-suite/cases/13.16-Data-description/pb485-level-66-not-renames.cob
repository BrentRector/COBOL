*>--- conformance-case
*> id: pb485-level-66-not-renames
*> rules: [SR-13.16.3-1, GR-13.18.33.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.33.4 GR2b: "Level-number 66 is assigned to identify
      *> RENAMES entries and may be used only as described by the renames
      *> format of the data description entry." This entry carries a
      *> PICTURE and no RENAMES clause, so it is a 13.16.2 format-1 entry
      *> written at level 66, and 13.16.3 SR1 bounds a format-1 entry to
      *> "77 or 1 through 49". The FORMAT axis is independent of the
      *> SECTION axis COBOLNET1746 screens: 66 is perfectly legal in
      *> working-storage under 13.18.33.3 SR5 -- as a RENAMES entry.
      *> kb/Work PB485.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB485N8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  V PIC X(3) VALUE "ABC".
       66  BAD PIC X(3).
       PROCEDURE DIVISION.
           DISPLAY V
           STOP RUN.
