      *>--- conformance-case
      *> id: pb485-level-number-unparseable
      *> rules: [SR-13.18.33.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.18.33.3 SR5 again, at the far end of the range: a
      *> twelve-digit level-number is a well-formed INTEGERLIT, so the
      *> grammar accepts it and the screen is the only thing that can
      *> refuse it. It used to become a SILENT lvl = 0 in DataBinder's
      *> bare int.TryParse -- the entry compiled and ran clean, exit 0.
      *> A value no int can hold is out of range for every one of the
      *> four arms by definition, so it takes the same diagnostic rather
      *> than a second mechanism. kb/Work PB485.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB485N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  V PIC X(4) VALUE "ABCD".
       999999999999 BAD PIC X(3).
       PROCEDURE DIVISION.
           DISPLAY V
           STOP RUN.
