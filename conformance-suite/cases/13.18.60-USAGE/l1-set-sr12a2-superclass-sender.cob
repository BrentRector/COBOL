      *>--- conformance-case
      *> id: l1-set-sr12a2-superclass-sender
      *> rules: [GR-13.18.60.4-22, SR-14.9.39.3-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.39.3 SR12 a)2. — "if the data item referenced by
      *> identifier-3 is described without an ONLY phrase, the
      *> object-class-name specified in the description of the data
      *> item referenced by identifier-4 shall reference the same
      *> class or a subclass of the class specified in the description
      *> of the data item referenced by identifier-3".
      *> `SET D TO B` is the NARROWING direction: identifier-3 is D,
      *> described OBJECT REFERENCE L1A2ND, and identifier-4 is B,
      *> described OBJECT REFERENCE L1A2NB.  L1A2NB is the SUPERCLASS
      *> of L1A2ND, so it is neither the same class as L1A2ND nor a
      *> subclass of it, and the SET violates a)2.  D carries no ONLY
      *> phrase, so a)2. — not a)1. — is the arm that applies, and
      *> neither operand carries FACTORY, so a)3. is satisfied and
      *> cannot be the reason for the rejection.
      *> This is the rejecting twin of 2002/l1_set_sr12a2_class_-
      *> conformance, whose `SET B TO D` is the same pair of items
      *> with the operands exchanged and which must COMPILE AND RUN;
      *> a compiler that rejected both, or accepted both, would fail
      *> exactly one of the pair.
      *> Reject-at names 2002 onward because object-reference data
      *> items and class definitions are COBOL-2002 constructs; the
      *> rule itself carries no edition condition.
      *> WHY THE .err ASSERTS THE RULE TAG AND NOT THE BARE CODE.
      *> COBOLNET0867 is emitted from seven distinct sites in the
      *> object-reference SET binder (SR9, SR8, SELF outside a
      *> method, SR12c2, SR10d1/d2, SR13 and the sender-shape
      *> fallback), and the widening check itself has a SECOND
      *> message for an UNRESOLVABLE class pair -- which is exactly
      *> how the forward/mutual class references below could
      *> plausibly misbehave.  A bare code would stay GREEN on any
      *> of them, and a shipped negative already asserts the bare
      *> COBOLNET0867 for a different rule.  The .err therefore
      *> holds `ISO SET SR12a2`, a contiguous substring of the
      *> conformance arm's own message; the code and the tag are
      *> NOT contiguous in the emitted text, so the tag is the
      *> discriminating half and the one that must be kept.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SETA2N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1A2NB
           CLASS L1A2ND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B USAGE OBJECT REFERENCE L1A2NB.
       01 D USAGE OBJECT REFERENCE L1A2ND.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE L1A2NB "NEW" RETURNING B.
           SET D TO B.
           STOP RUN.
       END PROGRAM L1SETA2N.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1A2NB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "BASE".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS L1A2NB.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1A2ND INHERITS FROM L1A2NB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1A2NB.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "DERIVED".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS L1A2ND.
