      *>--- conformance-case
      *> id: l1c21_propagate_default_off
      *> rules: [GR-7.3.21.4-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.21.4 GR4 — no PROPAGATE directive: nothing propagates
      *> "The default for a compilation group is PROPAGATE OFF."
      *>   cite.py --check 7.3.21.4 -> OK  §7.3.21.4 4)  (General rules)
      *> What ON would do instead (the behaviour the default excludes),
      *> §7.3.21.4 GR2: "any exception condition raised and not handled
      *> by either an exception phrase or exception processing
      *> procedures in that runtime element shall be propagated as
      *> though a GOBACK RAISING LAST statement were executed in a
      *> declarative for that exception condition."
      *>   cite.py --check 7.3.21.4 -> OK  §7.3.21.4 2)  (General rules)
      *> This compilation group has NO >>PROPAGATE directive, so both
      *> programs run with automatic propagation DISABLED.
      *> L1C21I raises EC-OVERFLOW-STRING (checking enabled by the TURN
      *> directive above): STRING "ABCDEF" DELIMITED BY SIZE INTO a
      *> PIC X(3) item, no ON OVERFLOW phrase, no declaratives.
      *>   §14.9.43.4 GR3c: the sending value is transferred "until all
      *>   data has been transferred or the end of the data item
      *>   referenced by identifier-3 has been reached" -> T = "ABC".
      *>   cite.py --check 14.9.43.4 -> OK  §14.9.43.4 3) c)  (General
      *>   rules)
      *>   §14.9.43.4 GR8d: "If the ON OVERFLOW phrase is not specified,
      *>   execution continues as specified in 14.6.13.1.4, Nonfatal
      *>   exception conditions."
      *>   cite.py --check 14.9.43.4 -> OK  §14.9.43.4 8) d)  (General
      *>   rules)
      *>   §14.6.13.1.4 (checking enabled): no conditional phrase (1),
      *>   no PERFORM WHEN (2), no applicable USE in L1C21I (3), so
      *>   item 4: "Execution of the statement continues as specified in
      *>   the rules for that statement."
      *>   cite.py --check 14.6.13.1.4 -> OK  §14.6.13.1.4 4)
      *>   (Nonfatal exception conditions)
      *> L1C21H has a USE AFTER EXCEPTION CONDITION EC-OVERFLOW-STRING
      *> declarative that would run if the exception were propagated to
      *> its CALL statement (the PROPAGATE ON behaviour).
      *> Expected output, derived:
      *>   "H CALLS I"                     — before the CALL.
      *>   "I CONTINUED ABC"               — OFF: L1C21I is not left by
      *>                                     an implied GOBACK RAISING
      *>                                     LAST; it continues (item 4)
      *>                                     with T = "ABC" (GR3c).
      *>   "I STATUS EC-OVERFLOW-STRING"   — the exception WAS raised
      *>                                     and unhandled: §15.33.3 the
      *>                                     last exception status name
      *>                                     (cite.py --check 15.33.3 ->
      *>                                     OK  §15.33.3 1)).
      *>   "H AFTER CALL"                  — L1C21H's declarative is NOT
      *>                                     entered: nothing was
      *>                                     propagated to its CALL.
      *>   (Under PROPAGATE ON the two "I" lines would be missing and an
      *>   "H DECLARATIVE" line would precede "H AFTER CALL".)
       >>TURN EC-OVERFLOW-STRING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C21H.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-OVERFLOW-STRING.
       H-PARA.
           DISPLAY "H DECLARATIVE".
       END DECLARATIVES.
       M-SEC SECTION.
       M-PARA.
           DISPLAY "H CALLS I"
           CALL "L1C21I"
           DISPLAY "H AFTER CALL"
           STOP RUN.
       END PROGRAM L1C21H.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C21I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC X(3) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STRING "ABCDEF" DELIMITED BY SIZE INTO T
           DISPLAY "I CONTINUED " T
           DISPLAY "I STATUS " FUNCTION EXCEPTION-STATUS
           GOBACK.
       END PROGRAM L1C21I.
