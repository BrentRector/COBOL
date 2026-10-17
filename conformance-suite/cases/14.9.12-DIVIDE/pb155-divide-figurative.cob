      *>--- conformance-case
      *> id: pb155-divide-figurative
      *> rules: [SR-14.9.12.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 8.8.1.1: ZERO is the only figurative constant admitted in
      *> an arithmetic expression (14.9.12.3 SR3's literal slots ride
      *> the same funnel). SPACE bound to a bare error node with NO
      *> diagnostic and surfaced as a RUNTIME NotImplemented - the
      *> wrong stage (kb/Work PB155).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB155N7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 8.
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE SPACE INTO N
           STOP RUN.
