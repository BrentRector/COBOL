      *>--- conformance-case
      *> id: l1-perform-f3-sr14-file-repeat-same-phrase
      *> rules: [SR-14.9.28.3-14]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.28.3 SR14 — one phrase against ITSELF. The counting
      *> domain the rule states is "in any of the WHEN phrases within
      *> the scope of a format 3 PERFORM statement", which is the
      *> statement's phrases taken together; two occurrences inside a
      *> single WHEN EXCEPTION file-name list are therefore still "more
      *> than once", and neither is paired with an exception-name, so
      *> the licensing clause does not apply.
      *> This case is what separates a per-STATEMENT census from a
      *> per-PHRASE one — a check that compared phrases pairwise would
      *> accept it.
      *> ONLY 2023 is named: the exception-checking PERFORM is new in
      *> COBOL-2023 (Annex E.3.3 item 36).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PFN14C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "l1pfn14c.txt".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R-F1 PIC X(5).
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               CONTINUE
           WHEN EXCEPTION F1 F1
               CONTINUE
           END-PERFORM.
           STOP RUN.
