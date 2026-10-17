      *>--- conformance-case
      *> id: l1-perform-f3-sr16-user-name
      *> rules: [SR-14.9.28.3-16]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.28.3 SR16 — the OPEN-FAMILY case, and the one that
      *> separates the rule the standard wrote from the two plausible
      *> substitutes. SR16 tests the WRITTEN NAME's leading characters;
      *> EC-USER-MINE is a legal level-3 user-defined exception-name
      *> (§14.6.13.1.1: "shall start with the characters 'EC-USER-' and
      *> end with a suffix containing only … basic letters, basic
      *> digits, and the hyphen and underscore"), so it resolves — and
      *> it still does not begin with 'EC-I-O', so pairing it with FILE
      *> violates SR16.
      *> An implementation that instead asked "is this name in the I-O
      *> CATEGORY / in the I-O part of the catalog" cannot decide this
      *> case at all, because the EC-USER- family is OPEN and cannot be
      *> enumerated — which is why this witness exists alongside
      *> l1-perform-f3-sr16-non-io-name.
      *> ONLY 2023 is named: the exception-checking PERFORM is new in
      *> COBOL-2023 (Annex E.3.3 item 36).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PFN16B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1pfn16b.txt".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R-F1 PIC X(5).
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               CONTINUE
           WHEN EC-USER-MINE FILE F1
               CONTINUE
           END-PERFORM.
           STOP RUN.
