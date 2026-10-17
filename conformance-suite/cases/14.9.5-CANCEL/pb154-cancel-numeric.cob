      *>--- conformance-case
      *> id: pb154-cancel-numeric
      *> rules: [SR-14.9.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.5.3 SR1: identifier-1 shall be defined as an
      *> alphanumeric or national data item - the CALL twin's screen
      *> (PB132) that CANCEL never got: a numeric target compiled clean
      *> and the statement was a silent no-op (kb/Work PB154).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB154N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NUM PIC 9(4) COMP.
       PROCEDURE DIVISION.
       MAIN.
           CANCEL WS-NUM
           STOP RUN.
