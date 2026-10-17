      *>--- conformance-case
      *> id: l1-delete-record-implicit-sequential-organization
      *> rules: [SR-14.9.10.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.10.3 SR1 with 12.4.5.10.3 GR6 - "When the
      *> ORGANIZATION clause is not specified, sequential organization
      *> with the RECORD SEQUENTIAL phrase is implied." F carries NO
      *> ORGANIZATION clause, so it IS a file with sequential
      *> organization, and SR1 therefore rejects the
      *> DELETE RECORD statement (COBOLNET0865). This is the arm a check
      *> written against an explicitly-written clause would miss - the
      *> sibling of l1-delete-record-sequential-organization.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DELN2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1deln2.dat"
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O F
           DELETE F RECORD
           CLOSE F
           STOP RUN.
