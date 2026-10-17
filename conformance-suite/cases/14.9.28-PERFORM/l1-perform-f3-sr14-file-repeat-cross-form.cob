      *>--- conformance-case
      *> id: l1-perform-f3-sr14-file-repeat-cross-form
      *> rules: [SR-14.9.28.3-14]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.28.3 SR14 — the CROSS-FORM occurrence. The rule
      *> opens "If file-name-1 OR file-name-2 is specified in a WHEN
      *> phrase, IT shall not be specified more than once in any of the
      *> WHEN phrases … unless ALL such instances are specified in
      *> conjunction with an exception-name." The pronoun is the FILE,
      *> not the grammar arm that produced it, so an occurrence as
      *> file-name-1 (the WHEN EXCEPTION {file-name-1}… arm) and an
      *> occurrence as file-name-2 (the exception-name FILE arm) are
      *> two occurrences of the same file — and because the first
      *> carries no exception-name, "all such instances" is false and
      *> the licence does not apply.
      *> ONLY 2023 is named: the exception-checking PERFORM is new in
      *> COBOL-2023 (Annex E.3.3 item 36).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PFN14B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1pfn14b.txt".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R-F1 PIC X(5).
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               CONTINUE
           WHEN EXCEPTION F1
               CONTINUE
           WHEN EC-I-O-AT-END FILE F1
               CONTINUE
           END-PERFORM.
           STOP RUN.
