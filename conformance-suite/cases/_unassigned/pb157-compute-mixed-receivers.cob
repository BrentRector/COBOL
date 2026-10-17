      *>--- conformance-case
      *> id: pb157-compute-mixed-receivers
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.8.3 SR1/SR2: COMPUTE's receivers are all numeric
      *> (Format 1) or all boolean (Format 2) - a mixed list satisfies
      *> neither. The reroute probe read only computeStore(0), so
      *> `COMPUTE N B = 1` never saw the boolean receiver and misrouted
      *> (kb/Work PB157 widened the probe to the whole list; the
      *> boolean channel then diagnoses both the non-boolean RHS and
      *> the numeric receiver).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB157N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 0.
       01 B PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N B = 1
           STOP RUN.
