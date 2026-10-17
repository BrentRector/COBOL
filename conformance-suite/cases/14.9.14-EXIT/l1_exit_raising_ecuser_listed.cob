      *>--- conformance-case
      *> id: l1_exit_raising_ecuser_listed
      *> rules: [SR-14.9.14.3-3]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.14.3 SR3, second paragraph — the POSITIVE CONTROL for
      *> negative/l1-exit-raising-ecuser-not-in-pd. When the level-3
      *> EC-USER exception-name IS "specified in the RAISING phrase of
      *> the procedure division header of the source element in which
      *> this EXIT statement is contained", SR3 is satisfied and the
      *> statement is legal. Without this control a compiler that
      *> refused EVERY EC-USER name would still pass the negative case.
      *> COMPILE-ONLY on purpose (no .out): what the statement DOES at
      *> run time is §14.9.14.4 GR2/GR3, a different rule. This pins
      *> only the acceptance half of SR3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1EXT04.
       PROCEDURE DIVISION RAISING EC-USER-L1LISTED.
       MAIN-P.
           EXIT PROGRAM RAISING EXCEPTION EC-USER-L1LISTED.
           STOP RUN.
