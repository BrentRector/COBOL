*>--- conformance-case
*> id: pb93-redefines-undefined-target
*> rules: [SR-13.18.44.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.44.3 SR4/SR7/SR10 place REDEFINES data-name-2 among the entries PRECEDING the subject at the same
      *> level, and 8.4.2.1 requires every reference to identify a resource: a data-name-2 that names nothing is an
      *> error at every edition (it used to compile silently — kb/Work PB93).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB93N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X.
       01 CPY-B REDEFINES NOPE PIC 9(2).
       PROCEDURE DIVISION.
           MOVE 1 TO CPY-B.
           STOP RUN.
