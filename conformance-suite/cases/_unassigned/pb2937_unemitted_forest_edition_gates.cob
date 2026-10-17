      *>--- conformance-case
      *> id: pb2937_unemitted_forest_edition_gates
      *> rules: []
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2937 - the edition pass's data gates read EVERY data
      *> forest the group binds, not only the forests it emits code
      *> for: an unexpanded parameterized class (OO deep-dive D12
      *> determination 6 - a definition all the same) and an interface's
      *> method prototypes. USAGE FLOAT-BINARY-32 / FLOAT-BINARY-64 are
      *> COBOL-2014 introductions (13.18.60.4 GR14 / GR15; cite.py
      *> --check 13.18.60.4 -> OK 13.18.60.4 14) / 15)), so at 2014 this
      *> group is conforming and runs; the converses at 2002 are
      *> negative/pb2937-skeleton-data-edition-gate and negative/pb2937-
      *> interface-formal-edition-gate.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB2937IF.
       PROCEDURE DIVISION.
       METHOD-ID. PUT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X USAGE FLOAT-BINARY-32.
       PROCEDURE DIVISION USING X.
       END METHOD PUT.
       END INTERFACE PB2937IF.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB2937SK USING T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS T.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FY USAGE FLOAT-BINARY-32.
       END FACTORY.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ITEM USAGE OBJECT REFERENCE T.
       01 Y USAGE FLOAT-BINARY-64.
       END OBJECT.
       END CLASS PB2937SK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2937M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Z USAGE FLOAT-BINARY-64.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1.5 TO Z
           IF Z = 1.5
               DISPLAY "PB2937-OK"
           END-IF
           STOP RUN.
       END PROGRAM PB2937M.
