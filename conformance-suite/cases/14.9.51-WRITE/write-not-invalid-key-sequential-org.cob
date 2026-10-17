*>--- conformance-case
*> id: write-not-invalid-key-sequential-org
*> rules: [SR-14.9.51.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.51.3 SR2, the NOT-alone spelling. The writeInvalidKey
      *> grammar rule has TWO alternatives -- INVALID first or NOT INVALID
      *> first -- and only the second reaches the screen through
      *> PhraseBlocks.StartsWithNot, so a fixture carrying INVALID KEY
      *> alone proves one alternative and leaves the other unmeasured.
      *> It matters here beyond message wording: NOT INVALID KEY is the
      *> arm with a LIVE meaning on this organization (9.1.14 final rule
      *> item 2 runs it on successful completion), so it is the arm a
      *> --permissive program will actually write (kb/Work PB691).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB691N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "pb691n2.dat"
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
               NOT INVALID KEY CONTINUE
           END-WRITE
           STOP RUN.
