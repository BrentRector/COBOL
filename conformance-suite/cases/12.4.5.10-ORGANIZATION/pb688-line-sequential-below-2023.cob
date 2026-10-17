*>--- conformance-case
*> id: pb688-line-sequential-below-2023
*> rules: [GR-12.4.5.10.3-2, FMT-12.4.5.10.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.4.5.10.3 GR2: "The LINE SEQUENTIAL phrase specifies that the file organization is line
      *> sequential."  The phrase is a COBOL-2023 INTRODUCTION: the Foreword's list of the main changes this
      *> third edition makes over ISO/IEC 1989:2014 names "Line Sequential file organization", and 9.1.6 /
      *> 9.1.7.1 still name exactly three organizations - sequential, relative and indexed - with 9.1.7.2
      *> splitting the sequential one into its record- and line-delimited types (the { LINE | RECORD }
      *> inner choice of the 12.4.5.10.2 general format).  Below 2023 the ORGANIZATION clause therefore has
      *> only SEQUENTIAL / RELATIVE / INDEXED, and the version-conformance pass rejects the LINE phrase with
      *> COBOLNET0900 (registry row file-organization-line-sequential-2023).  kb/Work PB688.
      *> This is the FILE-CONTROL site - the ISO general format's own position.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB688N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb688n1.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC PIC X(10).
       PROCEDURE DIVISION.
           OPEN OUTPUT F
           CLOSE F
           STOP RUN.
