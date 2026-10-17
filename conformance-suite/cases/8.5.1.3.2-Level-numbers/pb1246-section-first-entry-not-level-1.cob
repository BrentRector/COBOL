*>--- conformance-case
*> id: pb1246-section-first-entry-not-level-1
*> rules: [GR-8.5.1.3.2-3, GR-13.18.33.4-1, FMT-13.5.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1246 - ISO 13.11.1: "A record description entry consists of a set of data description entries, the first of which shall have level-number 1".
      *> cite.py: OK  13.11.1 (General).  13.18.33.4 GR1: "The level-number 1 identifies the first entry in each record description, type declaration, or report group."
      *> Here the working-storage section's first entry is level 05, so it opens no record; before the screen it rooted silently and the program ran.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB1246FIRSTNOTONE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       05  A PIC X VALUE "A".
       05  B PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY A B.
           STOP RUN.
