      *>--- conformance-case
      *> id: l1-perform-f3-sr16-non-io-name
      *> rules: [SR-14.9.28.3-16]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.28.3 SR16 — "If file-name-2 is specified,
      *> exception-name-2 shall begin with the COBOL characters
      *> 'EC-I-O'." EC-BOUND-SUBSCRIPT is a catalogued §14.6.13.1
      *> exception-name that does NOT begin with those characters, so
      *> pairing it with FILE violates the rule.
      *> The accept half is pinned by
      *> conformance:2023/l1_perform_f3_when_file_ec_io_prefix.
      *> ONLY 2023 is named: the exception-checking PERFORM is new in
      *> COBOL-2023 (Annex E.3.3 item 36).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PFN16A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1pfn16a.txt".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R-F1 PIC X(5).
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               CONTINUE
           WHEN EC-BOUND-SUBSCRIPT FILE F1
               CONTINUE
           END-PERFORM.
           STOP RUN.
