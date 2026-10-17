      *>--- conformance-case
      *> id: pb2937-skeleton-data-edition-gate
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2937 - an UNEXPANDED parameterized class is a class
      *> definition all the same (OO deep-dive D12 determination 6), so
      *> the edition pass's data gates ask its OBJECT data: USAGE
      *> FLOAT-BINARY-64 is a COBOL-2014 introduction (13.18.60.4 GR15;
      *> cite.py --check 13.18.60.4 -> OK 13.18.60.4 15)), and at 2002
      *> it is COBOLNET0900 whether or not anything expands the class.
      *> Nothing else here is below 2002, so the 0900 is this gate's.
       IDENTIFICATION DIVISION.
       CLASS-ID. NSKEDG USING T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS T.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ITEM USAGE OBJECT REFERENCE T.
       01 Y USAGE FLOAT-BINARY-64.
       END OBJECT.
       END CLASS NSKEDG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NSKEDGM.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM NSKEDGM.
