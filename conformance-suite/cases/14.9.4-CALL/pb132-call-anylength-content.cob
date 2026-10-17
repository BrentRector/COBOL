*>--- conformance-case
*> id: pb132-call-anylength-content
*> rules: [SR-14.9.4.3-18]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR18 (FORMAT 2): identifier-4 shall not be described with the ANY LENGTH clause -
      *> a prototype-less callee's formal cannot be proven ANY LENGTH (13.18.2.3 SR2 NOTE). The passer's
      *> own ANY LENGTH formal forwarded BY CONTENT is the reachable shape (kb/Work PB132).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB132N14.
       DATA DIVISION.
       LINKAGE SECTION.
       01 AL PIC X ANY LENGTH.
       PROCEDURE DIVISION USING AL.
       MAIN.
           CALL "S1" AS NESTED USING BY CONTENT AL
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LX PIC X(4).
       PROCEDURE DIVISION USING LX.
       P.
           GOBACK.
       END PROGRAM S1.
       END PROGRAM PB132N14.
