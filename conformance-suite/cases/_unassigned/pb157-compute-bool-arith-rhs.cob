      *>--- conformance-case
      *> id: pb157-compute-bool-arith-rhs
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.8 Format 2 / 8.8.2: a boolean receiver takes a
      *> BOOLEAN expression. The F1->F2 reroute's failure arm built the
      *> error NODE without a diagnostic, so this compiled clean and
      *> threw at run time - the PB68 sweep's sixth site (kb/Work
      *> PB157).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB157N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC 1(4) USAGE BIT.
       01 N PIC 9(4) VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE B = N + 1
           STOP RUN.
