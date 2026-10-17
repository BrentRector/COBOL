*>--- conformance-case
*> id: pb688-line-sequential-fd-site-below-2023
*> rules: [GR-12.4.5.10.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> The SECOND grammar site of the SAME clause.  COBOL.NET's grammar admits organizationClause from the
      *> file description entry as well as from the file control entry (CobolData.g4 fileDescriptionClause),
      *> so the 12.4.5.10.3 GR2 edition gate has TWO parents and one arm has to cover both - the shape this
      *> repo gets wrong most often (a two-arm dispatch with one arm fixed).  The rule is identical: the LINE
      *> SEQUENTIAL phrase is a COBOL-2023 introduction (the Foreword's main-changes list over ISO/IEC
      *> 1989:2014), so below 2023 it is COBOLNET0900 wherever it is written.  kb/Work PB688.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB688N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb688n2.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F
           ORGANIZATION IS LINE SEQUENTIAL.
       01  F-REC PIC X(10).
       PROCEDURE DIVISION.
           OPEN OUTPUT F
           CLOSE F
           STOP RUN.
