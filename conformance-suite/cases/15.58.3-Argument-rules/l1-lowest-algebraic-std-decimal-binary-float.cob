*>--- conformance-case
*> id: l1-lowest-algebraic-std-decimal-binary-float
*> rules: [AR-15.58.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.58.3 rule 2 — "If standard-decimal arithmetic is in effect, argument-1 shall not be a data
      *> item whose data description entry specifies a standard binary floating-point usage." §3.166 defines
      *> the term outright: "usages float-binary-32, float-binary-64, and float-binary-128". The OPTIONS
      *> paragraph writes ARITHMETIC IS STANDARD-DECIMAL (§11.9.5.1), which §11.9.5.2 GR3 puts in effect for
      *> this source element, so the FLOAT-BINARY-64 argument-1 below is illegal source: COBOLNET1516.
      *>
      *> THE CONDITION IS LOAD-BEARING, so this fixture is only half the evidence. The IDENTICAL item under
      *> the default NATIVE arithmetic (§11.9.5.2 GR4 — no ARITHMETIC clause, so NATIVE) is LEGAL and folds:
      *> conformance:cases/15.58.3-Argument-rules/l1_lowest_algebraic_value_rule, the line "FB64-NATIVE=OK". A compiler that simply
      *> refused every floating-point argument would pass THIS case and fail that one.
      *>
      *> ⚠ reject-at omits 85 and 2002: ARITHMETIC IS STANDARD-DECIMAL and USAGE FLOAT-BINARY-64 are both
      *> COBOL-2014 introductions (constructs.json arithmetic-standard-decimal-2014 /
      *> usage-float-binary64-2014), so below 2014 the program is rejected by the introduction gate instead —
      *> a different rule, and an .err match there would be a false green.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1LOWSD2.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FB64 USAGE FLOAT-BINARY-64.
       01 SR   PIC -9(6).9(3).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION LOWEST-ALGEBRAIC(FB64) TO SR.
           DISPLAY SR.
           STOP RUN.
       END PROGRAM L1LOWSD2.
