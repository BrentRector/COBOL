      *>--- conformance-case
      *> id: pb174-encoding-phrase-on-float-binary
      *> rules: [GR-13.18.60.4-20]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.18.60.2 general format: the encoding-phrase is printed
      *> only on FLOAT-DECIMAL-16 and FLOAT-DECIMAL-34, never on a
      *> standard BINARY float usage. 13.18.60.4 GR20a says the same in
      *> prose - it describes "any standard decimal floating-point
      *> usage". kb/Work PB174.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB174N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B USAGE FLOAT-BINARY-32 BINARY-ENCODING.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
