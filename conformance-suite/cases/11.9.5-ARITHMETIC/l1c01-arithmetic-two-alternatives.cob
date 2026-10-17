      *>--- conformance-case
      *> id: l1c01-arithmetic-two-alternatives
      *> rules: [FMT-11.9.5.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §11.9.5.1 format — ARITHMETIC IS with TWO alternatives
      *> THE FORMAT: ARITHMETIC IS {NATIVE | STANDARD-BINARY |
      *> STANDARD-DECIMAL} with plain braces (no choice indicators), so
      *> exactly ONE alternative shall be selected.
      *>   cite.py --check 11.9.5.1 "STANDARD-DECIMAL"
      *>     -> OK §11.9.5.1 (General format)
      *> NATIVE alone and STANDARD-DECIMAL alone are each legal at
      *> 2014/2023 (2014/l1c01_arithmetic_clause_forms); writing both is
      *> the only defect. The second phrase is an unexpected token in
      *> the OPTIONS paragraph: parse error COBOL0307.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C01I.
       OPTIONS.
           ARITHMETIC IS NATIVE STANDARD-DECIMAL.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
