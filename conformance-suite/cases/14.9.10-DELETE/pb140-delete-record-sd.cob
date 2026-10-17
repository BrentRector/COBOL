*>--- conformance-case
*> id: pb140-delete-record-sd
*> rules: [SR-14.9.10.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.4.6.3 SR3: a sort-merge file-name shall not be specified in
      *> an input-output statement - DELETE included (kb/Work PB140).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB140N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "s.dat".
       DATA DIVISION.
       FILE SECTION.
       SD S.
       01 SREC PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           DELETE S RECORD
           STOP RUN.
