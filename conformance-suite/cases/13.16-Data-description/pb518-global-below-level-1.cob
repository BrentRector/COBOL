*>--- conformance-case
*> id: pb518-global-below-level-1
*> rules: [SR-13.16.3-6, SR-13.18.27.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.16.3 SR6: "The CONSTANT RECORD and GLOBAL clauses may be specified only in data description
      *> entries whose level-number is 1." H is level 05. Before the screen the registration scan skipped it
      *> with a `continue`: the clause was accepted and then silently annulled (kb/Work PB518).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB518GBL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  H IS GLOBAL PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY H
           STOP RUN.
