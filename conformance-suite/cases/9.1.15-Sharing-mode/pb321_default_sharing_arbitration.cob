*>--- conformance-case
*> id: pb321_default_sharing_arbitration
*> rules: [GR-9.1.15-3, GR-14.9.27.4-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB321DEF.
      *> kb/Work PB321 — a file connector that writes NEITHER a SHARING
      *> clause nor an OPEN SHARING phrase still takes part in the ISO
      *> 14.9.27.4 Table 19 arbitration. 9.1.15 puts the gate on the
      *> physical file, not on the connectors that opted in: "Before access
      *> to a shared physical file is allowed through an OPEN statement, the
      *> sharing mode and the open mode of that OPEN statement shall be
      *> allowed by all other file connectors that are currently associated
      *> with the physical file".
      *>
      *> Such a connector's sharing mode is 9.1.15's implementor default
      *> ("If no specification is made in either location, the implementor
      *> defines the sharing mode in which the file is opened"), which
      *> COBOL.NET has not yet defined (kb/Work PB322). The arbitration
      *> therefore answers a conflict only where EVERY sharing mode the
      *> standard offers gives Table 19's "Unsuccessful open" — which for two
      *> undetermined connectors is exactly 9.1.13.9 1) e), the sub-case that
      *> names no sharing mode at all: "An attempt is made to open a physical
      *> file in the output mode and the physical file is currently open by
      *> another file connector."
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-SEED ASSIGN TO "pb321def.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SEED-ST.
           SELECT F-A ASSIGN TO "pb321def.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS A-ST.
           SELECT F-B ASSIGN TO "pb321def.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS B-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-SEED.
       01 SEED-REC PIC X(9).
       FD F-A.
       01 A-REC    PIC X(9).
       FD F-B.
       01 B-REC    PIC X(9).
       WORKING-STORAGE SECTION.
       01 SEED-ST  PIC XX.
       01 A-ST     PIC XX.
       01 B-ST     PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F-SEED.
           MOVE "SEEDVALUE" TO SEED-REC.
           WRITE SEED-REC.
           CLOSE F-SEED.
      *> Two INPUT readers: no candidate sharing mode makes this a conflict
      *> (Table 19 row SHARING WITH ALL OTHER / INPUT is "Normal open" in
      *> four of five columns), so the standard does not settle it against
      *> them and both opens succeed.
           OPEN INPUT F-A.
           OPEN INPUT F-B.
           DISPLAY "II-A=" A-ST " II-B=" B-ST.
           CLOSE F-A.
           CLOSE F-B.
      *> The OUTPUT request loses against ANY existing connector — every
      *> OUTPUT row of Table 19 is "Unsuccessful open" in all five columns.
           OPEN INPUT F-A.
           OPEN OUTPUT F-B.
           DISPLAY "IO-A=" A-ST " IO-B=" B-ST.
      *> 14.9.27.4 GR25 — "If the execution of the OPEN statement is
      *> unsuccessful, the file is not affected": the refused OPEN OUTPUT
      *> truncated nothing, so F-A still reads the seed record.
           READ F-A.
           DISPLAY "GR25-REC=" A-REC " ST=" A-ST.
           CLOSE F-A.
      *> 9.1.15 — "The file lock is removed by an explicit or implicit CLOSE
      *> statement executed for that file connector": with F-A closed the
      *> same OPEN OUTPUT now succeeds.
           OPEN OUTPUT F-B.
           DISPLAY "AFTER-CLOSE-B=" B-ST.
           CLOSE F-B.
           STOP RUN.
