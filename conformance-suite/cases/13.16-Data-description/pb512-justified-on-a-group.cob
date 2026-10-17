*>--- conformance-case
*> id: pb512-justified-on-a-group
*> rules: [SR-13.16.3-11, SR-13.18.32.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.16.3 SR11: "The PICTURE, JUSTIFIED, and BLANK WHEN ZERO clauses may be specified only for an
      *> elementary data item." G has a subordinate entry, so it is not elementary (8.5.1.3.1). Before the
      *> clause-placement screen the clause was accepted and INERT - the MOVE below stored 'ab   ', neither the
      *> right-justified value the clause names nor a diagnostic (kb/Work PB512). 85-era; all four editions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB512JOG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G JUSTIFIED RIGHT.
           05  A PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ab" TO G
           DISPLAY "G=[" G "]"
           STOP RUN.
