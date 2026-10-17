      *>--- conformance-case
      *> id: l1-test-formatted-datetime-format-not-literal
      *> rules: [AR-15.92.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 15.92.3 1): "Argument-1 shall be a national or
      *> alphanumeric literal." A DATA ITEM is not a literal. Nothing
      *> anywhere drives the rejection side of this rule under THIS
      *> function's name - the only spec-derived witness of the shared
      *> literal-ness arm cites 15.39.3 r1 under FORMATTED-DATE, a
      *> different rule row on a different function, so this row had
      *> no closing test at any edition.
      *> Argument-2 is of the same type as argument-1 (15.92.3 2)) and
      *> the format's content is a 15.3.1.2 basic calendar date
      *> format, so the only rule this program breaks is r1's
      *> literal-ness half.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGL1TFDNONLIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FMT PIC X(8) VALUE "YYYYMMDD".
       01 D   PIC X(8) VALUE "20210616".
       01 T   PIC 9(2).
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION TEST-FORMATTED-DATETIME(FMT D).
           STOP RUN.
