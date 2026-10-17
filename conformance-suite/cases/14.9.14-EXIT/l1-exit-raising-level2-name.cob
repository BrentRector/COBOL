      *>--- conformance-case
      *> id: l1-exit-raising-level2-name
      *> rules: [SR-14.9.14.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.14.3 SR3, first paragraph — "Exception-name-1 shall
      *> be a level-3 exception-name as specified in the rules for
      *> exception conditions specified in 14.6.13.1, Exception
      *> conditions."
      *> EC-BOUND is the LEVEL-2 family name of the EC-BOUND-* group
      *> (§14.6.13.1.1), not a level-3 exception-name, so this EXIT
      *> PROGRAM RAISING shall be rejected at every edition that has the
      *> RAISING phrase (a COBOL-2002 introduction; at 85 the phrase
      *> itself is the diagnostic, which is a different rule).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGL1XR1.
       PROCEDURE DIVISION.
       MAIN-P.
           EXIT PROGRAM RAISING EXCEPTION EC-BOUND.
           STOP RUN.
