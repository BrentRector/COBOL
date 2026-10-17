*>--- conformance-case
*> id: l1-lowest-algebraic-argument-1-shape
*> rules: [AR-15.58.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.58.3 rule 1 — "Argument-1 shall be a data item of category numeric or numeric-edited and
      *> shall not be an integer function or numeric function." FOUR violations, one for each clause of the
      *> rule; every one draws COBOLNET1516.
      *>   (a) AN PIC X(5) — category alphanumeric (§8.5.2.1 Table 2), neither category the rule admits.
      *>   (b) FUNCTION LENGTH — "The type of this function is integer" (§15.50.1): an INTEGER FUNCTION.
      *>   (c) FUNCTION ABS over a NON-integer numeric argument — §15.7.1's type table gives function type
      *>       Numeric for a Numeric argument (Integer only for an Integer one): a NUMERIC FUNCTION.
      *>   (d) FUNCTION L1-LOWFN — a user-defined function's returned value. The exclusion has to be WRITTEN
      *>       because §8.5.2.12 items 6/7 put "a numeric function" and "an integer function" in category
      *>       numeric, so the category half alone would admit exactly what the second half forbids.
      *> ⚠ TWO-ARM NOTE: conformance:cases/15.43.3-Argument-rules/algebraic-udf-argument pins (d) for the HIGHEST-ALGEBRAIC twin
      *> (§15.43.3 r1) only. The LOWEST-ALGEBRAIC arm of the same rule had no fixture of its own.
      *> The ADMIT side is conformance:cases/15.58.3-Argument-rules/l1_lowest_algebraic_value_rule (one numeric and two
      *> numeric-edited arguments folding), so a compiler that refused every argument would fail there while
      *> passing here.
      *> reject-at omits 85: LOWEST-ALGEBRAIC is not one of the 1989 Intrinsic Function Module's functions,
      *> so below 2002 the reference is rejected by the introduction gate instead — a different rule.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. L1-LOWFN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R-OUT PIC S9(4).
       PROCEDURE DIVISION RETURNING R-OUT.
           MOVE 7 TO R-OUT.
           GOBACK.
       END FUNCTION L1-LOWFN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1LOWARG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION L1-LOWFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AN  PIC X(5) VALUE "ABCDE".
       01 NV  PIC S9(4)V99 VALUE -12.50.
       01 R   PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION LOWEST-ALGEBRAIC(AN).
           COMPUTE R = FUNCTION LOWEST-ALGEBRAIC(FUNCTION LENGTH(AN)).
           COMPUTE R = FUNCTION LOWEST-ALGEBRAIC(FUNCTION ABS(NV)).
           COMPUTE R = FUNCTION LOWEST-ALGEBRAIC(FUNCTION L1-LOWFN).
           DISPLAY R.
           STOP RUN.
       END PROGRAM L1LOWARG.
