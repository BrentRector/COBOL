*>--- conformance-case
*> id: write-invalid-key-sequential-org
*> rules: [SR-14.9.51.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.51.3 SR2: "If the organization of the write file is
      *> sequential, format 1 shall be specified." Format 1 of 14.9.51.2
      *> carries no INVALID KEY bracket at all -- only Format 2 (random)
      *> does -- so the phrase is not admissible on a WRITE whose file
      *> has sequential organization, at every edition.
      *> This arm bound through SequentialIoBinder.BindWrite, which never
      *> read writeInvalidKey: the phrase was PARSED, DROPPED, and neither
      *> imperative ran, with no diagnostic at any edition. It is the third
      *> arm of one silent drop -- PB144 screened REWRITE, PB334 READ --
      *> and it hid because the writeInvalidKey sub-rule was consumed by
      *> the KEYED binder only (kb/Work PB691).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB691N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "pb691n1.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD S.
       01 S-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT S
           MOVE "AAAA" TO S-REC
           WRITE S-REC
               INVALID KEY CONTINUE
           END-WRITE
           STOP RUN.
