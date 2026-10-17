*>--- conformance-case
*> id: delete-record-sequential-org
*> rules: [SR-14.9.10.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.10.3 SR1: "The DELETE RECORD statement shall not be
      *> specified for a file with sequential organization."
      *> This is a HARD error at every edition and strictness -- NOT a
      *> leniency, and deliberately not routed through the COBOLNET1720
      *> seam. It had no negative fixture at all: COBOLNET0865 occurred
      *> exactly ONCE in the repository, at its own emit site, so the rule
      *> was enforced and never proved (kb/Work PB144).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB144N5.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "n5.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD S.
       01 S-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O S
           READ S
           DELETE S RECORD
           STOP RUN.
