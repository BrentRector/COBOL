*>--- conformance-case
*> id: l1-exp10-class-numeric
*> rules: [AR-15.35.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.35.3 r1 — "Argument-1 shall be of class numeric." EXP10's OWN argument rule.
      *>
      *> --check validated:
      *>   cite.py --check 15.35.3 "Argument-1 shall be of class numeric." -> OK §15.35.3 1)
      *> §8.5.2.1 Table 2 puts an alphanumeric PICTURE item in class ALPHANUMERIC, so PIC X(4) is
      *> not of class numeric and this reference is illegal source; §4.2.2 obliges the indication.
      *> The diagnostic is COBOLNET1627 ("intrinsic-argument-class", docs/DIAGNOSTICS.md).
      *>
      *> ⛔ THE SIBLING THAT WAS NEVER WITNESSED. negative/pb12-exp-alphanumeric-argument pins the
      *> IDENTICAL rule for EXP (§15.34.3 r1) and its header names EXP10 as carrying the same
      *> text - but the fixture calls EXP alone. The only EXP10 negative in the corpus,
      *> negative/intrinsic-phrase-word-argument, rejects FUNCTION EXP10(LEADING) with
      *> COBOLNET1638, which is the reserved-PHRASE-WORD screen and not the §15.3 class screen:
      *> a reserved word never reaches the class question at all. So EXP10's class rule had a row
      *> in IntrinsicArgumentRules.Verified and no test - the two-arm shape where only one arm was
      *> ever measured.
      *>
      *> ⚠ 85 IS NOT IN THE REJECT SET: EXP10 carries IntroducedIn 2002, so at --std 85 the
      *> COBOLNET1502 edition window fires first and the program is rejected for a DIFFERENT
      *> reason. Collapsing the two obligations would let a regression in either hide behind the
      *> other (the argument negative/standard-binary-e-2002 makes for its own gate).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1EXP10CLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC X(4) VALUE "ABCD".
       01 W-R PIC S9(9)V9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W-R = FUNCTION EXP10(W-A).
           STOP RUN.
