      *>--- conformance-case
      *> id: l1c18-merge-file-not-sort-merge
      *> rules: [SR-14.9.24.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.24.3 SR2 — MERGE file-name-1 described by an
      *> ordinary FD (not a sort-merge file description entry).
      *> Rule: "File-name-1 shall be described in a sort-merge file
      *> description entry in the data division."
      *> cite.py: OK  §14.9.24.3 2)  (Syntax rules)
      *> F3 is a sequential FD; its key F3-REC is in F3's record
      *> (SR4 a) satisfied), F1/F2/FO are ordinary FDs as SR9 wants,
      *> no name repeats (SR7), so only SR2 is violated.  Diagnostic
      *> COBOLNET1757 with the MERGE sort-merge message head.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C18C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1c18c1.dat".
           SELECT F2 ASSIGN TO "l1c18c2.dat".
           SELECT F3 ASSIGN TO "l1c18c3.dat".
           SELECT FO ASSIGN TO "l1c18co.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC        PIC X(5).
       FD  F2.
       01  F2-REC        PIC X(5).
       FD  F3.
       01  F3-REC        PIC X(5).
       FD  FO.
       01  FO-REC        PIC X(5).
       PROCEDURE DIVISION.
       MAIN-P.
           MERGE F3 ON ASCENDING KEY F3-REC
               USING F1 F2 GIVING FO.
           STOP RUN.
