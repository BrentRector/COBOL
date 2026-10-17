      *>--- conformance-case
      *> id: l1c11-select-not-first
      *> rules: [SR-12.4.5.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.2 SR1 — the SELECT clause shall be first
      *> Rule: "The SELECT clause shall be specified first in the file
      *>   control entry."
      *>   cite.py --check 12.4.5.2 "The SELECT clause shall be
      *>   specified first in the file control entry."
      *>   -> OK  §12.4.5.2 1)  (Syntax rules)
      *> The file control entry for F-OUT opens with its ASSIGN clause
      *> and names the file in a SELECT clause written second; the
      *> same entry with SELECT first is legal, so the only defect is
      *> the clause order this rule forbids. Pure-syntax rule: the
      *> expected diagnostic is the parser's unexpected-token error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C11N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           ASSIGN TO "L1C11N1.DAT" SELECT F-OUT.
       DATA DIVISION.
       FILE SECTION.
       FD F-OUT.
       01 OUT-REC PIC X(5).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F-OUT.
           CLOSE F-OUT.
           STOP RUN.
