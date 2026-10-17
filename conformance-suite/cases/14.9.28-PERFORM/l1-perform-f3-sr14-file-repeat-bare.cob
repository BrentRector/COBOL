      *>--- conformance-case
      *> id: l1-perform-f3-sr14-file-repeat-bare
      *> rules: [SR-14.9.28.3-14]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.28.3 SR14 — "If file-name-1 or file-name-2 is
      *> specified in a WHEN phrase, it shall not be specified more
      *> than once in any of the WHEN phrases within the scope of a
      *> format 3 PERFORM statement unless all such instances are
      *> specified in conjunction with an exception-name."
      *> F1 is specified in TWO WHEN phrases and NEITHER instance is
      *> paired with an exception-name, so the licensing clause does
      *> not apply and the statement is illegal.
      *> The licence half is pinned by
      *> conformance:2023/l1_perform_f3_when_file_repeat_licensed.
      *> ONLY 2023 is named: the exception-checking PERFORM is new in
      *> COBOL-2023 (Annex E.3.3 item 36), so below 2023 the whole
      *> format is rejected by the construct gate with a different
      *> diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PFN14A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1pfn14a.txt".
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
           WHEN EXCEPTION F1
               CONTINUE
           END-PERFORM.
           STOP RUN.
