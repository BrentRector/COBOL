      *>--- conformance-case
      *> id: rewrite-invalid-key-relative-sequential
      *> rules: [SR-14.9.35.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.35.3 SR2 SECOND ARM: "... or a file with relative
      *> organization and sequential access mode." The twin of
      *> rewrite-invalid-key-sequential-org -- both arms of ONE rule, so
      *> both are pinned (kb/Work PB144).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB144N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R ASSIGN TO "n3.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS R-K.
       DATA DIVISION.
       FILE SECTION.
       FD R.
       01 R-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 R-K PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O R
           READ R
           REWRITE R-REC
               INVALID KEY CONTINUE
           END-REWRITE
           STOP RUN.
