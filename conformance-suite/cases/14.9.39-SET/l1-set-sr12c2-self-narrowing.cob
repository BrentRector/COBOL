      *>--- conformance-case
      *> id: l1-set-sr12c2-self-narrowing
      *> rules: [SR-14.9.39.3-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.39.3 SR12 c)2. — with the predefined object
      *> reference SELF as identifier-4 and an object-class-name
      *> receiver, "the class containing the SET statement shall be
      *> the same class or a subclass of the class specified in the
      *> description of the data item referenced by identifier-3".
      *> `SET R TO SELF` here sits in an instance method of L1C2NB
      *> while R is described OBJECT REFERENCE L1C2ND, and L1C2ND
      *> INHERITS FROM L1C2NB.  The class containing the SET is
      *> therefore the SUPERCLASS of identifier-3's class — neither
      *> the same class nor a subclass of it — so c)2. is violated.
      *> R carries no ONLY phrase (c)1. satisfied) and no FACTORY
      *> phrase, and the SET is in the INSTANCE definition (c)3.
      *> satisfied), so c)2. is the only rule left to fail and the
      *> rejection cannot be attributed to a sibling.
      *> This is the rejecting twin of
      *> 2002/l1_set_sr12c2_self_conformance, whose SUBCLASS leg is
      *> the same shape with the inheritance direction reversed and
      *> which must COMPILE AND RUN.
      *> Reject-at names 2002 onward because class definitions and
      *> object-reference data items are COBOL-2002 constructs; the
      *> rule itself carries no edition condition.
      *> WHY THE .err ASSERTS THE RULE TAG AND NOT THE BARE CODE.
      *> COBOLNET0867 is emitted from seven distinct sites in the
      *> object-reference SET binder, so a bare code cannot say
      *> WHICH of the format-5 obligations rejected the program --
      *> a shipped negative already asserts that bare code for
      *> SR10d2.  The .err holds `SR12c2`, a contiguous substring
      *> of the c)2. arm's own message.
      *> RUN-TIME WATCH FOR THE DIRECTOR: the c)2. arm fires only
      *> once the receiver's object-class-name RESOLVES in the
      *> class table.  L1C2ND is named in L1C2NB's REPOSITORY -- a
      *> class naming its own SUBCLASS -- and if that failed to
      *> resolve the binder would fall through to the interface arm
      *> and emit NOTHING.  That shows up as the "must be
      *> REJECTED" assertion failing, not as a false green.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SETC2N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C2NB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE L1C2NB.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE L1C2NB "NEW" RETURNING O.
           INVOKE O "GRAB".
           STOP RUN.
       END PROGRAM L1SETC2N.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C2NB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS L1C2ND.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R USAGE OBJECT REFERENCE L1C2ND.
       PROCEDURE DIVISION.
       METHOD-ID. GRAB.
       PROCEDURE DIVISION.
       MAIN.
           SET R TO SELF.
       END METHOD GRAB.
       END OBJECT.
       END CLASS L1C2NB.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C2ND INHERITS FROM L1C2NB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C2NB.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "PING".
       END METHOD PING.
       END OBJECT.
       END CLASS L1C2ND.
