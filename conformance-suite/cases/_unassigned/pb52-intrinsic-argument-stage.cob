*>--- conformance-case
*> id: pb52-intrinsic-argument-stage
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.84.3 r1: "Argument-1 shall be of class numeric." A figurative
      *> SPACE is an alphanumeric character value (8.3.3.6.4 GR1), so this is
      *> illegal source - and it was decidable at BIND while being reported at
      *> RUN TIME as "figurative 'S' in a numeric context" (fix-queue PB52).
      *>
      *> SQRT was simply absent from IntrinsicArgumentRules.Verified, a
      *> deliberately fail-open table that a function joins only once its 15.x
      *> rule has been READ AND CITED. The queue entry named SQRT; COS, SIN, TAN
      *> and SIGN carry the IDENTICAL rule and were equally unscreened, so all
      *> five joined together, each clause read individually.
      *>
      *> WATCH THE CLAUSE NUMBER: SQRT is 15.84, NOT 15.81 - 15.81 is SIGN. A
      *> first pass read 15.81.3's "shall be of class numeric" and took it for
      *> SQRT's. The text was real and the clause belonged to another function.
      *>
      *> AND WATCH THE FIXTURE NAME NEXT DOOR: pb1-numeric-arg-trig-family covers
      *> ACOS, ASIN and ATAN - the three INVERSE functions - and its comment says
      *> so honestly. Its NAME does not: a reader scanning the corpus for "is the
      *> trig family screened?" would have read it as yes, while COS, SIN and TAN
      *> sat unscreened. The gap was never claimed, only implied by a name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB52NEGSQRT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(6)V99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N = FUNCTION SQRT(SPACE).
           DISPLAY "N=" N.
           STOP RUN.
