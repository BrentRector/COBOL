*>--- conformance-case
*> id: pb511-external-as-below-2002
*> rules: [FMT-13.16.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB511 — the BELOW-INTRODUCTION half. The EXTERNAL clause's `AS literal-1` is the shared
      *> externalizedNamePhrase, and that phrase is a COBOL-2002 introduction: X3.23-1985 prints it in no
      *> general format and makes AS a user-definable word (the user-word-as-2002 twin). So at --std 85 this
      *> program draws the ONE introduction gate every AS site shares — VersionConformancePass
      *> ParseArm.VisitExternalizedNamePhrase → constructs.json externalized-name-as-2002 → COBOLNET0900 —
      *> and NOT the bare `COBOLNET0901: 'AS' is a reserved word` the phrase drew before it had a grammar.
      *> BOTH ARMS ARE WRITTEN HERE ON PURPOSE (§13.4.5.2 Formats 1/2/3 and §13.16.2 Format 1 print the same
      *> `[ IS EXTERNAL [ AS literal-1 ] ]` slot): the FD arm and the data-description arm are one clause,
      *> and a gate that reached only one of them would be the repo's two-arm defect shape again.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB511B85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT NF ASSIGN TO "pb511-neg.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  NF IS EXTERNAL AS "PB511NF".
       01  NF-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  NW IS EXTERNAL AS "PB511NW" PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
