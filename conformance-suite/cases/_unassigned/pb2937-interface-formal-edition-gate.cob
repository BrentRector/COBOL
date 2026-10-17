      *>--- conformance-case
      *> id: pb2937-interface-formal-edition-gate
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2937 (the sibling arm) - an interface's method
      *> prototypes declare data, and the edition pass's data gates ask
      *> it as they ask a class's: USAGE FLOAT-BINARY-32 is a COBOL-2014
      *> introduction (13.18.60.4 GR14; cite.py --check 13.18.60.4 ->
      *> OK 13.18.60.4 14)), so a prototype formal described with it is
      *> COBOLNET0900 at 2002. No interface forest was ever gated.
      *> Nothing else here is below 2002, so the 0900 is this gate's.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. NIFEDG.
       PROCEDURE DIVISION.
       METHOD-ID. PUT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X USAGE FLOAT-BINARY-32.
       PROCEDURE DIVISION USING X.
       END METHOD PUT.
       END INTERFACE NIFEDG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NIFEDGM.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM NIFEDGM.
