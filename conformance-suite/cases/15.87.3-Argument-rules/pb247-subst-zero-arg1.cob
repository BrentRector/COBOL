*>--- conformance-case
*> id: pb247-subst-zero-arg1
*> rules: [AR-15.87.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB247 - ISO 15.87.3 r3's ARGUMENT-1 half: "Neither argument-1 nor argument-2 shall be of zero
      *> length". Both pre-existing r3 fixtures (negative/pb58-subst-zero, negative/pb58-subst-zero2) target an
      *> argument-2, so the argument-1 half - the schema's MinWidth(1) predicate on position 0, evaluated through
      *> PredicateViolation on BOTH of BindSubstitute's exits - rested on code reading alone. A zero-length
      *> alphanumeric literal is a STATIC width, so this is the predicate's own reachable case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGSUBSTZEROA1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A20   PIC X(20).
       PROCEDURE DIVISION.
           MOVE FUNCTION SUBSTITUTE("" "A" "B") TO A20.
           STOP RUN.
