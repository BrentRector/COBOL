*>--- conformance-case
*> id: pb31-max-mixed-argument-classes
*> rules: [AR-15.59.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.59.3 r2 (and 15.63.3 r2 for MIN): "All arguments shall be of the
      *> same class with the exception that mixing of arguments of alphabetic and
      *> alphanumeric classes is allowed." A numeric argument-1 and an
      *> alphanumeric argument-2 are two classes, so the LIST is illegal even
      *> though r1 admits each argument on its own.
      *>
      *> IT WAS ACCEPTED (fix-queue PB31). The screen checked every argument
      *> INDEPENDENTLY and had no cross-argument shape at all, so each position
      *> passed and the illegal list sailed through. The candidate-class set
      *> model makes the rule an INTERSECTION: {numeric} and {alphanumeric} share
      *> nothing, so the list is rejected — while MAX(ZERO "A") stays legal,
      *> because 8.3.3.6.4 GR4 lets ZERO be either.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB31NEGMAX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC S9(9)V99.
       PROCEDURE DIVISION.
           COMPUTE N = FUNCTION MAX(1 "A")
           STOP RUN.
