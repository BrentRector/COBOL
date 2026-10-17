*>--- conformance-case
*> id: pb65_domain_guards
*> rules: [AR-15.9.3-2, AR-15.74.3-2, AR-15.75.3-2, DOC-A.1-90]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.9.3 r2/r3, §15.74.3 r2, §15.75.3 r2 — the financial/random argument-domain guards
      *> (fix-queue PB65). A negative ANNUITY rate, a PRESENT-VALUE rate ≤ −1, and a negative RANDOM
      *> seed are incorrect argument VALUES: §15.3 sets EC-ARGUMENT-FUNCTION and the documented
      *> default is 0 (checking off here). The old bodies computed silently — ANNUITY(-0.5 3) gave
      *> +0.0714, and RANDOM(-5) aliased a positive seed through a mask. The legal control
      *> ANNUITY(0, 4) = 1/4 (§15.9.4 r1a). Both carriers guard through ONE raise site per rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65DOM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R  PIC S9(4)V9(4) SIGN LEADING SEPARATE.
       01 RR PIC 9.9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION ANNUITY(-0.5, 3)
           DISPLAY "AN-NEG=" R
           COMPUTE R = FUNCTION PRESENT-VALUE(-2.00, 300)
           DISPLAY "PV-NEG=" R
           COMPUTE RR = FUNCTION RANDOM(-5)
           DISPLAY "RD-NEG=" RR
           COMPUTE R = FUNCTION ANNUITY(0, 4)
           DISPLAY "AN-OK =" R
           STOP RUN.
