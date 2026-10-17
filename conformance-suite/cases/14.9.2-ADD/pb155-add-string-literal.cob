      *>--- conformance-case
      *> id: pb155-add-string-literal
      *> rules: [SR-14.9.2.3-3, SR-14.9.12.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.2.3 SR3 / 8.8.1.1: literals in arithmetic shall be
      *> NUMERIC literals; both formats of the alphanumeric literal are
      *> of class and category alphanumeric (8.3.3.2.1). This fell
      *> through to the numeric-literal path carrying its QUOTED text
      *> and died as a raw Roslyn error - the wrong stage (kb/Work
      *> PB155, the PB94 VALUE-clause fix's arithmetic sibling).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB155N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           ADD "ABC" TO N
           STOP RUN.
