*>--- conformance-case
*> id: pb1569-type-bit-group-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1569 — the NEGATIVE twin of conformance-suite/cases/13.18.1-ALIGNED/pb1569_type_subject_level1_bit_alignment.
      *> The level-1 alignment ISO §13.18.57.4 GR2 d) gives a group TYPE subject is reachable only through the
      *> TYPEDEF/TYPE family (§13.18.58 / §13.18.57), GROUP-USAGE BIT (§13.18.29) and USAGE BIT (§13.18.60) —
      *> COBOL-2002 introductions, none of which exists in COBOL-85 — each gated by COBOLNET0900 at --std 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1569NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BT TYPEDEF GROUP-USAGE BIT.
           05  B1          PIC 1 USAGE BIT.
           05  B2          PIC 1 USAGE BIT.
       01  R.
           05  F           PIC 1 USAGE BIT.
           05  S TYPE BT.
       PROCEDURE DIVISION.
           STOP RUN.
