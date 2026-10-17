      *>--- conformance-case
      *> id: l1_set_sr12a2_class_conformance
      *> rules: [SR-14.9.39.3-12]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.39.3 SR12 a)2. — SET format 5 (object-reference
      *> assignment) where identifier-3 is described with an
      *> object-class-name and WITHOUT an ONLY phrase: "the
      *> object-class-name specified in the description of the data
      *> item referenced by identifier-4 shall reference the same
      *> class or a subclass of the class specified in the description
      *> of the data item referenced by identifier-3".
      *> Neither receiver below carries ONLY, so the rule's own guard
      *> holds and a)2. — not a)1. — governs; neither side carries
      *> FACTORY, so a)3. is satisfied by both being absent.
      *>
      *> BOTH ADMITTED LEGS ARE EXERCISED, and the two DISPLAY lines
      *> are what separates them:
      *>   SET B TO D  — the SUBCLASS leg.  D is described
      *>     OBJECT REFERENCE L1A2D and L1A2D INHERITS FROM L1A2B, so
      *>     identifier-4's class is a subclass of identifier-3's.
      *>   SET B TO B2 — the SAME-CLASS leg.  Both are described
      *>     OBJECT REFERENCE L1A2B.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULE TEXT AND ITS
      *> NEIGHBOURS, NOT FROM A RUN.  §14.9.39.4 GR9: "If
      *> identifier-4 is specified, a reference to the object
      *> identified by identifier-4 is placed into each data item
      *> referenced by identifier-3" — so after `SET B TO D` the
      *> reference held in B identifies the L1A2D instance, and after
      *> `SET B TO B2` it identifies an L1A2B instance.  §14.9.23.4
      *> GR2: "Identifier-1 identifies an instance object … literal-1
      *> … identifies a method OF THAT OBJECT that will act upon that
      *> instance object" — the method is resolved on the object, not
      *> on the receiver's DECLARED class.  Hence DERIVED then BASE.
      *> DISCRIMINATOR: a widening that copied only the base part of
      *> the object, or that resolved SPEAK statically on B's declared
      *> class L1A2B, would print BASE twice.
      *>
      *> The REJECTING twin is negative/l1-set-sr12a2-superclass-sender
      *> (the narrowing direction, which a)2. forbids); the two differ
      *> only in which operand is the subclass, which is what makes
      *> the pair a test of the RULE rather than of the program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SETA2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1A2B
           CLASS L1A2D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B USAGE OBJECT REFERENCE L1A2B.
       01 B2 USAGE OBJECT REFERENCE L1A2B.
       01 D USAGE OBJECT REFERENCE L1A2D.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE L1A2D "NEW" RETURNING D.
           SET B TO D.
           INVOKE B "SPEAK".
           INVOKE L1A2B "NEW" RETURNING B2.
           SET B TO B2.
           INVOKE B "SPEAK".
           STOP RUN.
       END PROGRAM L1SETA2.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1A2B INHERITS FROM BASE.
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
       END CLASS L1A2B.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1A2D INHERITS FROM L1A2B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1A2B.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "DERIVED".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS L1A2D.
