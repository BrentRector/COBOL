*>--- conformance-case
*> id: pb316-open-sharing-all-no-lockmode
*> rules: [SR-14.9.27.3-8, GR-14.9.27.4-20]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB316 - the COMPLEMENT of the per-group relaxation. Making
      *> 14.9.27.3 SR8 per group must not stop it firing on the file whose
      *> OWN group carries the ALL phrase: "if the sharing phrase is
      *> omitted from the OPEN statement and the ALL phrase is specified in
      *> the SHARING clause of the file control entry for file-name-1 or if
      *> the ALL phrase is specified on the OPEN statement, the LOCK MODE
      *> clause shall be specified in the file control entry for
      *> file-name-1". Here the ALL phrase IS on F2's own group and F2 has
      *> no LOCK MODE clause, so SR8 is violated - and the sibling group's
      *> F1, which does have a LOCK MODE clause, must not launder it.
      *> (The file control CLAUSE arm of SR8 is negative/sharing-all-no-lockmode.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB316NEGSR8.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb316n1.dat"
               ORGANIZATION IS SEQUENTIAL
               LOCK MODE IS MANUAL.
           SELECT F2 ASSIGN TO "pb316n2.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(5).
       FD F2.
       01 R2 PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SHARING WITH ALL OTHER F1
                OUTPUT SHARING WITH ALL OTHER F2.
           STOP RUN.
