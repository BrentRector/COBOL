      *>--- conformance-case
      *> id: pb157-compute-all-literal
      *> rules: [SR-14.9.8.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.8.3 SR3: a boolean-compute expression shall not
      *> consist solely of the figurative constant ALL literal. The
      *> ACTUAL prohibition was never pinned - only the bare-ZERO
      *> misfire was (kb/Work PB51/PB157; a bare ZERO is now legal
      *> per 8.3.3.6.3 SR1a while the ALL-written form still rejects).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB157N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE B = ALL B"1"
           STOP RUN.
