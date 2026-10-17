      *>--- conformance-case
      *> id: pb155-unstring-numeric-delimiter
      *> rules: [SR-14.9.48.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.48.3 SR2 names identifier-1, -2, -3 AND -5 in one
      *> sentence: category alphanumeric or national. The sweep that
      *> screened the sender left a numeric DELIMITED BY operand
      *> binding clean (kb/Work PB155).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB155NA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(6) VALUE "A1B2C3".
       01 ND PIC 9(3) VALUE 1.
       01 R PIC X(6).
       PROCEDURE DIVISION.
       MAIN.
           UNSTRING S DELIMITED BY ND INTO R
           STOP RUN.
