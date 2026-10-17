*>--- conformance-case
*> id: pb518-external-fd-filler-record
*> rules: [SR-13.16.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.16.3 SR7, its second half: "The data-name format of the entry-name clause shall be specified
      *> ... for record descriptions associated with a file description entry that contains the EXTERNAL or
      *> GLOBAL clause." The record below is FILLER. Accepted with no diagnostic before (kb/Work PB518).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB518EFF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb518eff.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F IS EXTERNAL.
       01  FILLER PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OK"
           STOP RUN.
