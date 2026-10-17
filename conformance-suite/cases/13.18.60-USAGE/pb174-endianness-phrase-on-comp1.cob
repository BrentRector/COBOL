      *>--- conformance-case
      *> id: pb174-endianness-phrase-on-comp1
      *> rules: [GR-13.18.60.4-19]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.18.60.2 general format (verified against the printed
      *> page): the endianness-phrase is printed only on the five
      *> STANDARD floating-point usages. COMP-1 is not one of them
      *> (3.166/3.167), and 13.18.60.4 GR19c scopes the implied phrase
      *> the same way - GR13/GR21 leave COMP-1's representation to the
      *> implementor. kb/Work PB174.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB174N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C USAGE COMP-1 HIGH-ORDER-LEFT.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
