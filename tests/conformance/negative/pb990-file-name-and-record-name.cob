      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB990 - ISO 8.3.2.2 (cite.py --check 8.3.2.2 "Within a source element, a given user-defined word may
      *> be used as only one type of user-defined word"): KF is a file-name (SELECT, FD) AND the record-name of the
      *> 01 under its FD. file-name is not in exception 3's group (constant-name, data-name, property-name,
      *> record-key-name, record-name), so the two cannot be one word.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB990C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT KF ASSIGN TO "NEGPB990C.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  KF.
       01  KF PIC X(4).
       PROCEDURE DIVISION.
           STOP RUN.
