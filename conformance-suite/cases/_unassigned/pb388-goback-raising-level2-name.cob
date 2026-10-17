      *>--- conformance-case
      *> id: pb388-goback-raising-level2-name
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.18.3 SR2, first sentence — "Exception-name-1 shall be
      *> a level-3 exception-name as specified in 14.6.13.1, Exception
      *> conditions."
      *> EC-BOUND is the LEVEL-2 family name of the EC-BOUND-* group, so
      *> the RAISING phrase of this GOBACK statement may not name it.
      *>
      *> ⛔ THE SECOND ARM (kb/Work PB388). The EXIT twin of this fixture
      *> is l1-exit-raising-level2-name.cob, and ONE code path binds
      *> both: EcBinder.EcBindRaising over EcNameResolution.TryResolve.
      *> The rule it enforces is written down once per statement under a
      *> DIFFERENT ordinal each time — EXIT §14.9.14.3 SR3, GOBACK
      *> §14.9.18.3 SR2, RAISE §14.9.29.3 SR1 — so the message printed
      *> RAISE's rule number at every one of them. The pair pins both
      *> arms: each .err names the citation its own statement carries,
      *> and a single hard-coded clause cannot satisfy both.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB388-GOBACK-LEVEL2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "UNREACHABLE".
           GOBACK RAISING EXCEPTION EC-BOUND.
