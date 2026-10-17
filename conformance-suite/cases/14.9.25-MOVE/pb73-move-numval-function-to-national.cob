*>--- conformance-case
*> id: pb73-move-numval-function-to-national
*> rules: [SR-14.9.25.3-10, GR-15.2-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.25.3 Table 16: Noninteger numeric -> National is "No"; NUMVAL is a NUMERIC function (15.67.1), so
      *> its temporary is the Noninteger row whatever the argument's value (8.4.3.2.3 SR11). kb/Work PB73.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB73NVN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N10 PIC N(10).
       PROCEDURE DIVISION.
           MOVE FUNCTION NUMVAL("12") TO N10.
           STOP RUN.
