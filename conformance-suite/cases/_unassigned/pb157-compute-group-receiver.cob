      *>--- conformance-case
      *> id: pb157-compute-group-receiver
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.8.3 SR2: a boolean COMPUTE receiver shall be an
      *> ELEMENTARY boolean data item. An ordinary alphanumeric group
      *> is neither (a GROUP-USAGE BIT group is - 13.18.29.4 GR1b - and
      *> the positive golden pins that boundary's other side).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB157N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GX.
          05 F1X PIC X(2).
          05 F2X PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE GX = B"1"
           STOP RUN.
