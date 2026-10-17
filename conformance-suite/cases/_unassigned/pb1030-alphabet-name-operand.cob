*>--- conformance-case
*> id: pb1030-alphabet-name-operand
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1030 - ISO 8.4.2.1: a statement's reference shall uniquely identify the resource
      *> it uses. An alphabet-name identifies no data item; DISPLAY AL used to compile with a "not
      *> implemented" warning and abort the run unit.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1030D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 E PIC X OCCURS 5.
       01 X PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
           DISPLAY AL.
           STOP RUN.
