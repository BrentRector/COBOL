*>--- conformance-case
*> id: pb134-divide-remainder-no-giving
*> rules: [FMT-14.9.12.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB134 second pass - the remaining non-format shapes (COBOLNET1689): no ADD/SUBTRACT
      *> format prints operands with both phrases absent; DIVIDE's REMAINDER formats print GIVING with
      *> exactly ONE quotient (14.9.12.2 Formats 4-5 / SR6). Each was a RUNTIME loud before.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB134N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(4).
       01 B PIC 9(4).
       01 C PIC 9(4).
       01 D PIC 9(4).
       01 R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE A INTO B REMAINDER R
           STOP RUN.
