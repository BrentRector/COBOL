      *>--- conformance-case
      *> id: l1c10_file_control_two_entries
      *> rules: [FMT-12.4.4.2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.4.2 format — FILE-CONTROL with repeated entries
      *>
      *> Row pinned: FMT-12.4.4.2 (the ellipsis arm: more than one
      *> file-control-entry in one paragraph).
      *>  General format: "FILE-CONTROL. [ file-control-entry ] ..."
      *>  The ellipsis lets the bracketed entry repeat, so two SELECT
      *>  entries under one FILE-CONTROL header are legal and each
      *>  declares its own file (§12.4.5: "The file control entry
      *>  declares the relevant physical attributes of a file.").
      *> cite.py --check:
      *>  OK  §12.4.4.2   (General format)  file-control-entry
      *>  OK  §12.4.5   (File control entry)
      *> Derivation: each file is written with one record and read
      *> back through its own entry; if the second entry were lost or
      *> merged with the first, the second READ could not return its
      *> own record.
      *>  F1=AAAA   record written to / read from file L1C10F1
      *>  F2=BBBB   record written to / read from file L1C10F2
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C10D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT L1C10F1 ASSIGN TO "L1C10D1.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT L1C10F2 ASSIGN TO "L1C10D2.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD L1C10F1.
       01 R1 PIC X(4).
       FD L1C10F2.
       01 R2 PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT L1C10F1 L1C10F2.
           MOVE "AAAA" TO R1.
           WRITE R1.
           MOVE "BBBB" TO R2.
           WRITE R2.
           CLOSE L1C10F1 L1C10F2.
           MOVE SPACES TO R1 R2.
           OPEN INPUT L1C10F1 L1C10F2.
           READ L1C10F1.
           READ L1C10F2.
           DISPLAY "F1=" R1.
           DISPLAY "F2=" R2.
           CLOSE L1C10F1 L1C10F2.
           STOP RUN.
