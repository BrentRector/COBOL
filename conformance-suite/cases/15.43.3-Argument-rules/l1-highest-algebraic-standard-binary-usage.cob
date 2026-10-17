*>--- conformance-case
*> id: l1-highest-algebraic-standard-binary-usage
*> rules: [AR-15.43.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.43.3 r2 — "If standard-decimal arithmetic is in effect, argument-1 shall not be a data item
      *> whose data description entry specifies a standard binary floating-point usage."
      *> (cite.py --check 15.43.3 "If standard-decimal arithmetic is in effect, argument-1 shall not be a
      *> data item whose data description entry specifies a standard binary floating-point usage" -> OK,
      *> §15.43.3 rule 2.)
      *>
      *> BOTH HALVES OF THE ANTECEDENT ARE WRITTEN HERE, EXPLICITLY. "Standard-decimal arithmetic is in
      *> effect" because the OPTIONS paragraph says ARITHMETIC IS STANDARD-DECIMAL (§11.9.5). "A standard
      *> binary floating-point usage" is §3.166's defined term — "usages float-binary-32, float-binary-64,
      *> and float-binary-128" — so USAGE FLOAT-BINARY-64 is one, and COMP-1 / COMP-2 / FLOAT-SHORT /
      *> FLOAT-LONG / FLOAT-EXTENDED are NOT. That distinction is the whole point of the fixture: the guard
      *> that stood before kb/Work R10 + PB122 rejected every float under every mode and told the user, in
      *> the diagnostic text, that COMP-2 was "barred by rule 2 under STANDARD-DECIMAL" — a false statement
      *> of this rule shipped in a user-visible message.
      *>
      *> BOTH EDITIONS ARE REACHABLE. ARITHMETIC IS STANDARD-DECIMAL (§11.9.5 / §8.8.1.5) and the §3.166
      *> usages (§13.18.60.4 GR14-GR16) are both COBOL-2014 additions, so the rule as worded cannot be
      *> evaluated at COBOL-2002 at all and the header names 2014 and 2023 only.
      *>
      *> THE ADMIT ARM IS conformance:cases/15.43.3-Argument-rules/l1_algebraic_standard_binary_usage_mode_bar — the same USAGE
      *> FLOAT-BINARY-32/-64 items under NATIVE arithmetic, where r2's antecedent is false and the reference
      *> must return a value. A screen that ignored the mode would fail that fixture; one that ignored the
      *> usage family would fail conformance:cases/15.43.3-Argument-rules/pb122_smallest_algebraic_float, whose COMP-1/COMP-2
      *> arguments run under STANDARD-DECIMAL.
      *> Expected: COBOLNET1516, the algebraic-family argument diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGL1HASB.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FB64 USAGE FLOAT-BINARY-64.
       01 R    PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION HIGHEST-ALGEBRAIC(FB64).
           DISPLAY R.
           STOP RUN.
       END PROGRAM NEGL1HASB.
