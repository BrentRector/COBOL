*>--- conformance-case
*> id: pb256-standard-binary-test-numval-f
*> rules: [RV-15.95.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB256 — the TEST-NUMVAL-F twin of standard-binary-test-numval-2014.
      *> §15.95.4 r1 b) 3 ("If standard-binary arithmetic is in effect, the argument contains a
      *> significand longer than 35 digits …"), b) 5 and b) 6 each name STANDARD-BINARY beside
      *> STANDARD-DECIMAL, and §15.69.3 r3's first sentence bounds that mode's significand at 35.
      *> ARITHMETIC IS STANDARD-BINARY is DECLINED (owner decision D-A, protocol 2026-08-30,
      *> kb/Work PB198): Annex A.3 item 2 makes the clause processor-dependent and ISO §4.2.6 gives
      *> the implementor the discretion not to claim support, plus the duty to warn at compile time
      *> and to document the absence (docs/CONFORMANCE.md §1/§2). So those half-rules are UNREACHABLE
      *> rather than unwritten — and, exactly as its TEST-NUMVAL sibling says, the decline is proven
      *> here ON THE PATH the §15.95 rows are about rather than on the OPTIONS clause in isolation.
      *> The 35 itself is recorded in ArithmeticModes.NumvalDigitCap so the table has no hole.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SBTNVF2014.
       OPTIONS.
           ARITHMETIC IS STANDARD-BINARY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R PIC 9(4)V9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1.5E+3").
           DISPLAY R.
           STOP RUN.
