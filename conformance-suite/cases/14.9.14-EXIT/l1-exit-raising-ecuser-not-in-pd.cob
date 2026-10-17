      *>--- conformance-case
      *> id: l1-exit-raising-ecuser-not-in-pd
      *> rules: [SR-14.9.14.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.14.3 SR3, second paragraph — "If exception-name-1 is
      *> a level-3 exception-name for EC-USER, exception-name-1 shall be
      *> specified in the RAISING phrase of the procedure division
      *> header of the source element in which this EXIT statement is
      *> contained."
      *> EC-USER-L1NOPD is a level-3 EC-USER name (§14.6.13.1.1, the
      *> open EC-USER family) and this procedure division header carries
      *> NO RAISING phrase, so the statement shall be rejected.
      *> POSITIVE CONTROL: 2023/l1_exit_raising_ecuser_listed.cob — the
      *> same shape with the name LISTED in the header shall compile.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGL1XR2.
       PROCEDURE DIVISION.
       MAIN-P.
           EXIT PROGRAM RAISING EXCEPTION EC-USER-L1NOPD.
           STOP RUN.
