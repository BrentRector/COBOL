      *>--- conformance-case
      *> id: pb316-open-sharing-group-below-2002
      *> rules: [FMT-14.9.27.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB316 - the edition gate on the OPEN SHARING phrase, in
      *> the MULTI-GROUP form. The phrase is a COBOL-2002 introduction
      *> (ISO 14.9.27); it is superset-parsed at every edition and gated
      *> after binding, and since PB316 each repeated group carries its
      *> own phrase - so the gate has to see a phrase that is NOT on the
      *> statement's first group. negative/sharing_below_2002 covers the
      *> file control SHARING clause; this covers the OPEN phrase.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB316GATE85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb316g1.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F2 ASSIGN TO "pb316g2.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(5).
       FD F2.
       01 R2 PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F1 OUTPUT SHARING WITH NO OTHER F2.
           STOP RUN.
