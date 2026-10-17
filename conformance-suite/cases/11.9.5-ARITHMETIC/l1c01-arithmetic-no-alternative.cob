      *>--- conformance-case
      *> id: l1c01-arithmetic-no-alternative
      *> rules: [FMT-11.9.5.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §11.9.5.1 format — ARITHMETIC IS with NO alternative
      *> THE FORMAT: ARITHMETIC IS {NATIVE | STANDARD-BINARY |
      *> STANDARD-DECIMAL}; the braces require one alternative, so
      *> "ARITHMETIC IS." is not a clause. (The 2002 format, ARITHMETIC
      *> IS {NATIVE | STANDARD}, likewise requires one.)
      *>   cite.py --check 11.9.5.1 "STANDARD-DECIMAL"
      *>     -> OK §11.9.5.1 (General format)
      *> The OPTIONS paragraph is otherwise valid; the missing phrase is
      *> parse error COBOL0001 (missing token).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C01J.
       OPTIONS.
           ARITHMETIC IS.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
