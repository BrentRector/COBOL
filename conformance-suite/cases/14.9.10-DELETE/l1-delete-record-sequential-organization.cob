      *>--- conformance-case
      *> id: l1-delete-record-sequential-organization
      *> rules: [SR-14.9.10.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.10.3 SR1 - "The DELETE RECORD statement shall not be
      *> specified for a file with sequential organization." F is
      *> written ORGANIZATION IS SEQUENTIAL (12.4.5.10.3 GR3), so the
      *> DELETE RECORD statement is rejected (COBOLNET0865). The rule
      *> carries no edition delta, so every edition rejects.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DELN1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1deln1.dat"
               ORGANIZATION IS SEQUENTIAL
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
