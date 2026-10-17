*>--- conformance-case
*> id: pb1053-dynamic-members-both-sides-at-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1053 - a record whose dynamic-length members flank a
      *> fixed member is sent with its extent table (determination D-FRA
      *> (v), docs/CONFORMANCE.md section 3), but the DYNAMIC LENGTH clause
      *> itself is a COBOL-2014 addition (ISO 8.5.1.10 / 13.18.19), so
      *> below 2014 the record is refused by the edition gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1053NEG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1053neg.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 A PIC X DYNAMIC LENGTH LIMIT 5.
          05 K PIC X(3).
          05 C PIC X DYNAMIC LENGTH LIMIT 5.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           MOVE "AA" TO A MOVE "KEY" TO K MOVE "CCCC" TO C
           WRITE R
           CLOSE F
           STOP RUN.
