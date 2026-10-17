*>--- conformance-case
*> id: signed_float_literal_argument
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R17 (ledger F12) - a SIGNED floating-point literal is one argument, in both spellings.
      *> ISO/IEC 1989:2023 8.3.3.3.3 r2: "The literal to the left of the 'E' represents the significand.
      *> It may be signed" - the sign is part of the LITERAL, so FUNCTION EXP(-1.5E1) is ONE argument
      *> (15.3 type 10; 8.4.3.2.3 SR8 admits a literal argument). Before R17 the signed-decimal lexer
      *> rule won maximal munch at "-1.5" and orphaned "E1" as an identifier, so the reference drew a
      *> false COBOLNET1504 arity error; the keyword-omitted spelling (8.4.3.2.3 SR2) failed its outer
      *> capture the same way. Expected values are the IEEE binary64 evaluations (8.8.1.3 native;
      *> 15.34.4 r1's EAE over a float argument evaluates in binary64 - CONFORMANCE.md item 92).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R17SFLOAT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R USAGE COMP-2.
       01 S USAGE COMP-2.
       01 M USAGE COMP-2.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION EXP(-1.5E1).
           DISPLAY "NEG=" R.
           COMPUTE S = EXP(+1.5E1).
           DISPLAY "KOF=" S.
           COMPUTE M = FUNCTION MAX(-1.5E3, 2).
           DISPLAY "MAX=" M.
           STOP RUN.
