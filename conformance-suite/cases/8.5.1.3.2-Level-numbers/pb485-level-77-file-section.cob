*>--- conformance-case
*> id: pb485-level-77-file-section
*> rules: [GR-8.5.1.3.2-2, SR-13.18.33.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.33.3 SR2: "Data description entries subordinate to a
      *> FD or SD entry shall have level-numbers with the values 66, 88,
      *> or 1 through 49." 77 is ABSENT from that set and present in SR5's
      *> -- the sets are NOT the same set, which is why the screen is
      *> section-keyed. 13.18.33.4 GR2a is the reason: level-number 77
      *> identifies NONCONTIGUOUS working storage, local storage and
      *> linkage items, and a record area has no such thing.
      *> This case is the witness that a single flat "1-49, 66, 77 or 88"
      *> test -- the shape the legacy engine's CBL0815 message states --
      *> would ACCEPT: it is the arm a section-blind screen gets wrong.
      *> kb/Work PB485.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB485N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb485n3.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC PIC X(10).
       77  F-BAD PIC X(3).
       PROCEDURE DIVISION.
           OPEN OUTPUT F
           CLOSE F
           STOP RUN.
