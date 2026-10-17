      *>--- conformance-case
      *> id: l1-formatted-time-format-not-literal
      *> rules: [AR-15.41.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 15.41.3 1): "Argument-1 shall be a national or
      *> alphanumeric literal." A DATA ITEM is not a literal, so a
      *> program that names one there is not conforming and shall be
      *> diagnosed. This is the LITERAL half of the rule, and nothing
      *> under this function's name exercised it: the only
      *> spec-derived witness of the shared literal-ness arm in the
      *> tree drives it under FORMATTED-DATE's 15.39.3 r1 row, a
      *> different rule on a different function.
      *> The format's CONTENT is a perfectly good time format
      *> (15.3.3.1, extended common time), so the rejection can only
      *> come from the literal-ness screen and never from the
      *> 15.41.3 2) content screen.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGL1FTNONLIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FMT PIC X(8) VALUE "hh:mm:ss".
       01 R   PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION FORMATTED-TIME(FMT 45296) TO R.
           STOP RUN.
