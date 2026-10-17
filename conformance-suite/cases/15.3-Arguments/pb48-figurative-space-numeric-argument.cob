*>--- conformance-case
*> id: pb48-figurative-space-numeric-argument
*> rules: [AR-15.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.3.3.6.3 SR1a: "If the literal is restricted to a numeric literal, the
      *> only figurative constant permitted is ZERO (ZEROS, ZEROES) without the ALL
      *> phrase." 8.3.3.6.4 GR5 gives SPACE a CHARACTER reading only - unlike GR4,
      *> which gives ZERO a numeric one as well - and 8.8.1.1 names exactly one
      *> figurative constant among the operands an arithmetic expression may be
      *> built from, which is again ZERO. 15.7.3 r1 requires ABS's argument to be of
      *> class numeric, so SPACE is inadmissible here.
      *>
      *> IT COMPILED CLEAN AND ABORTED AT RUN TIME on "figurative 'S' in a numeric
      *> context" (fix-queue PB48). The class screen classified every operand by the
      *> ONE class it has, and a figurative constant has none - so it fell out of
      *> the screen entirely as "not statically decidable" and fail-open let it
      *> through to the emitter. The screen now asks which classes the operand CAN
      *> present and intersects, which admits ZERO in both families and admits SPACE
      *> in neither numeric one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB48NEGSPACE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC S9(4)V99.
       PROCEDURE DIVISION.
           COMPUTE N = FUNCTION ABS(SPACE)
           STOP RUN.
