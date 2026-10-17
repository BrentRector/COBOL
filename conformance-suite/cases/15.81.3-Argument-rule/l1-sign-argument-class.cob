*>--- conformance-case
*> id: l1-sign-argument-class
*> rules: [AR-15.81.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.81.3 rule 1 — "Argument-1 shall be of class numeric." Two violations, both refused by the
      *> §15.3 argument-class screen with COBOLNET1627:
      *>   (a) AN PIC X(5) — class alphanumeric (§8.5.2.1 Table 2).
      *>   (b) NE PIC ZZ9.99 — CLASS, not category: §8.5.2.1 Table 2 files NUMERIC-EDITED under class
      *>       ALPHANUMERIC when its usage is display, so a numeric-edited item is not of class numeric
      *>       however numeric it looks. §15.3's type 10 (Numeric) admits "an arithmetic expression or a
      *>       numeric data item", and this is neither.
      *>
      *> ⚠ WHY THIS FIXTURE EXISTS. SIGN's reject side had never been executed on a bad argument: it was
      *> pinned only through a SIBLING — conformance:cases/_unassigned/pb52-intrinsic-argument-stage exercises
      *> FUNCTION SQRT(SPACE) over the same argument-rule table and the same CheckArgumentClasses path — so
      *> the row's evidence was that a NEIGHBOUR is screened, never that SIGN is.
      *> The ADMIT side is conformance:cases/15.7.4-Returned-value-rule/pb2_float_argument_exact_family, where FUNCTION SIGN over a
      *> COMP-2 item (class numeric) returns -1.
      *>
      *> reject-at omits 85: SIGN is not one of the 1989 Intrinsic Function Module's functions and was
      *> introduced by ISO/IEC 1989:2002, so below 2002 the reference draws the introduction gate instead —
      *> a different rule, and matching the .err there would be a false green.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SIGNCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AN PIC X(5) VALUE "ABCDE".
       01 NE PIC ZZ9.99.
       01 R  PIC S9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION SIGN(AN).
           COMPUTE R = FUNCTION SIGN(NE).
           DISPLAY R.
           STOP RUN.
       END PROGRAM L1SIGNCLS.
