*>--- conformance-case
*> id: pb201-refmod-group-bound
*> rules: [SR-8.4.3.3.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> The SIBLING position (kb/Work PB201, and the two-arm law): 8.4.3.3.3
      *> rule 4 - "Leftmost-position and length shall be arithmetic expressions" -
      *> carries 8.8.1.1 to a reference-modification bound exactly as 8.4.2.3.2
      *> carries it to a subscript, so an alphanumeric GROUP (8.5.2.1: "an
      *> alphanumeric group item has class and category alphanumeric") is
      *> inadmissible here for the same reason and at every edition.
      *> Measured on e4850fc7 under --permissive: CS1503 '_T_0' to 'long'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB201N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WG.
          05 WF1 PIC 9(2) VALUE 2.
       01 W  PIC X(5) VALUE "ABCDE".
       01 R2 PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE W(WG:2) TO R2
           STOP RUN.
