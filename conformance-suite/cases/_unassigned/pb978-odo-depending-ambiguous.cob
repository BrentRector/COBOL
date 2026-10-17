*>--- conformance-case
*> id: pb978-odo-depending-ambiguous
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB978 - ISO 8.4.2.2.3 SR1: "For each non unique user-defined name that is explicitly
      *> referenced, uniqueness shall be established through a sequence of qualifiers that precludes any
      *> ambiguity of reference." CNT is declared under GA and under GB, neither in T's own record, and
      *> DEPENDING ON CNT is unqualified. The resolver used to take the first declared (GA's) and run.
      *> Expected: COBOLNET1639 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB978NOD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GA.
          05 CNT PIC 9 VALUE 2.
       01 GB.
          05 CNT PIC 9 VALUE 5.
       01 T.
          05 E PIC X OCCURS 1 TO 9 DEPENDING ON CNT.
       PROCEDURE DIVISION.
           MOVE "ABCDEFGHI" TO T
           DISPLAY T
           STOP RUN.
