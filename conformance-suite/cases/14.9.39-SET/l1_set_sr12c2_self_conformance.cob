*>--- conformance-case
*> id: l1_set_sr12c2_self_conformance
*> rules: [SR-14.9.39.3-12]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 SR12 c)2. — SET format 5 with the predefined
      *> object reference SELF as identifier-4 and an
      *> object-class-name receiver: "the class containing the SET
      *> statement shall be the same class or a subclass of the class
      *> specified in the description of the data item referenced by
      *> identifier-3".
      *> Neither receiver carries ONLY, so c)1. is satisfied; both
      *> SETs sit in an INSTANCE method and neither receiver carries
      *> FACTORY, so c)3. is satisfied and c)4. does not apply.  The
      *> only rule under test is therefore c)2., and BOTH of the
      *> classes it admits are exercised:
      *>   SET R  TO SELF in L1C2B, R  described OBJECT REFERENCE
      *>     L1C2B  — the SAME-CLASS leg.
      *>   SET R-DERIVED TO SELF in L1C2D, R-DERIVED described OBJECT REFERENCE
      *>     L1C2B, L1C2D INHERITS FROM L1C2B — the SUBCLASS leg.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULE TEXT AND ITS
      *> NEIGHBOURS.  §8.4.3.8.4 GR1: "SELF and SUPER both reference
      *> the object that was used to invoke the method in which the
      *> reference to SELF or SUPER appears."  §14.9.39.4 GR9: a
      *> reference to the object identified by identifier-4 is placed
      *> into the data item referenced by identifier-3.  §14.9.23.4
      *> GR2: the invoked method is a method OF THAT OBJECT.  So
      *> INVOKE O "GRAB" runs in an L1C2B object and prints BASE, and
      *> INVOKE P "GRABD" runs in an L1C2D object whose SPEAK is the
      *> OVERRIDE and prints DERIVED — even though R-DERIVED's declared
      *> class is the base class L1C2B.
      *> DISCRIMINATOR: resolving SPEAK on R-DERIVED's DECLARED class, or
      *> copying only the base part of the object across the widening,
      *> would print BASE twice; only the subclass leg's line moves.
      *>
      *> The REJECTING twin is negative/l1-set-sr12c2-self-narrowing,
      *> where the SET sits in the BASE class and the receiver is
      *> described with the SUBCLASS — the direction c)2. forbids.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SETC2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C2B
           CLASS L1C2D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE L1C2B.
       01 P USAGE OBJECT REFERENCE L1C2D.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE L1C2B "NEW" RETURNING O.
           INVOKE O "GRAB".
           INVOKE L1C2D "NEW" RETURNING P.
           INVOKE P "GRABD".
           STOP RUN.
       END PROGRAM L1SETC2.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C2B INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS L1C2B.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R USAGE OBJECT REFERENCE L1C2B.
       PROCEDURE DIVISION.
       METHOD-ID. GRAB.
       PROCEDURE DIVISION.
       MAIN.
           SET R TO SELF.
           INVOKE R "SPEAK".
       END METHOD GRAB.

       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "BASE".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS L1C2B.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C2D INHERITS FROM L1C2B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C2B.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R-DERIVED USAGE OBJECT REFERENCE L1C2B.
       PROCEDURE DIVISION.
       METHOD-ID. GRABD.
       PROCEDURE DIVISION.
       MAIN.
           SET R-DERIVED TO SELF.
           INVOKE R-DERIVED "SPEAK".
       END METHOD GRABD.

       METHOD-ID. SPEAK OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "DERIVED".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS L1C2D.
