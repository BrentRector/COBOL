*>--- conformance-case
*> id: intrinsic_exp_overflow
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> CA24 (CONFORMANCE-FIX-QUEUE): EXP/EXP10's only argument rule is "class numeric" (ISO 15.34.3 r1) — no upper
      *> bound — so EXP(710) is a LEGAL argument whose value e**710 ~ 2.25e308 merely overflows; it saturates and
      *> undergoes ordinary receiver SIZE handling (15.34.4 / 14.7.4), it is NOT a domain error. Pre-fix, +Inf was
      *> mapped to EC-ARGUMENT-FUNCTION's default 0, so NOT ON SIZE ERROR wrongly fired and R became 0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA24.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(5) VALUE 7.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION EXP(710)
               ON SIZE ERROR DISPLAY "SIZE"
               NOT ON SIZE ERROR DISPLAY "OK".
           DISPLAY R.
           STOP RUN.
