*>--- conformance-case
*> id: pb451_set_format5_self_sender
*> rules: [GR-11.4.4-2, GR-11.8.4-2, SR-14.9.39.3-10, SR-14.9.39.3-12]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 — SET format 5 with the predefined object reference SELF as the sender.
      *> The RECEIVER's §13.18.60.2 description picks the rule: SR10 d) for an interface-name
      *> receiver, SR12 c) for an object-class-name receiver.  Both are keyed to the FACTORY /
      *> INSTANCE axis, because SELF is the FACTORY object inside a factory method and an
      *> INSTANCE object inside an instance method.  kb/Work PB451.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES — NOT FROM A RUN:
      *>
      *> 1-2. INVOKE PB451P "FMK" runs a method of PB451P's FACTORY definition.
      *>    SET FR TO SELF, with FR described FACTORY OF PB451P, is SR12 c): c)1. is satisfied
      *>    (no ONLY phrase), c)2. "the class containing the SET statement shall be the same
      *>    class or a subclass of the class specified in the description" (PB451P is PB451P),
      *>    and c)4. "if the data item referenced by identifier-3 is described with a FACTORY
      *>    phrase, the method containing the SET statement shall be defined in the factory
      *>    definition of its containing class" — it is.  INVOKE FR "TAG" resolves the FACTORY
      *>    roster (§9.3.6):                                                          FTAG-P
      *>    SET JR TO SELF, with JR described with interface-name PB451J, is SR10 d)1.: "if
      *>    the SET statement is contained in a method within the factory definition of the
      *>    class, that factory definition shall be described with an IMPLEMENTS clause that
      *>    references int-1."  PB451P's FACTORY carries IMPLEMENTS PB451K, and PB451K
      *>    INHERITS FROM PB451J, so §11.4.4 GR2 b) ("the factory object implements an
      *>    interface that inherits int-1") makes it implement PB451J — the relation §11.4.4
      *>    GR1 says the clause names.  INVOKE JR "PING":                            FPING-P
      *>
      *> 3. INVOKE PB451Q "QFMK" runs a method of PB451Q's FACTORY definition, which carries
      *>    NO IMPLEMENTS clause at all; only its base PB451P's does.  §11.4.4 GR2 c) — "the
      *>    class containing the factory object inherits a class whose factory object
      *>    implements int-1" — is the leg that makes SR10 d)1. hold here.  PB451Q's FACTORY
      *>    overrides PING:                                                         FPING-Q
      *>    DISCRIMINATOR: this is the sharpest of the SR10 d) legs — a definition with no
      *>    IMPLEMENTS clause AT ALL — so a literal reading of "described with an IMPLEMENTS
      *>    clause that references int-1" refuses it, and printing FPING-P instead would mean
      *>    SELF had been resolved to the BASE factory object rather than the active one.
      *>
      *> 4-5. INVOKE OP "MK" runs an INSTANCE method of PB451P on a PB451P object.
      *>    SET O2 TO SELF, O2 described with the object-class-name PB451P, is SR12 c)2. plus
      *>    c)3. "if the data item referenced by identifier-3 is described without a FACTORY
      *>    phrase, the method containing the SET statement shall be defined in the instance
      *>    definition of its containing class" — it is.  INVOKE O2 "WHO":            WHO-P
      *>    SET J2 TO SELF is SR10 d)2., the instance twin, over §11.8.4 GR2 b):     IPING-P
      *>
      *> 6-7. INVOKE OQ "QMK" runs an INSTANCE method of PB451Q on a PB451Q object.
      *>    SET PR TO SELF, PR described with the object-class-name PB451P, is SR12 c)2.'s
      *>    SUBCLASS leg: the class containing the SET (PB451Q) is a subclass of PB451P.
      *>    INVOKE PR "WHO" dispatches on the object actually held:                   WHO-Q
      *>    SET J2 TO SELF is SR10 d)2. over §11.8.4 GR2 c) — PB451Q's OBJECT carries no
      *>    IMPLEMENTS clause; its base PB451P's does.  INVOKE J2 "PING":           IPING-Q
      *>
      *> 8. SET OQ TO NULL is SR12 d), the fourth member of the rule's closed list of
      *>    senders — "the predefined object reference NULL" — into a receiver described
      *>    with an object-class-name.  §13.18.60.4 GR22 makes null a legal content of
      *>    every object reference ("It shall contain either null or a reference to an
      *>    object"), so the assignment is unconditional: no class relation, no FACTORY or
      *>    ONLY axis, nothing to check.  §8.8.4.2.15 then observes it — "An operand of
      *>    class object may be compared with another operand of class object", true when
      *>    both reference the same object:                                       NULLOK
      *>    DISCRIMINATOR: the line runs after OQ has held a real PB451Q object, so it
      *>    proves the SET cleared it rather than reporting an initial value.
      *>
      *> The rejecting arms are fixtures, one clause each:
      *> conformance:cases/14.9.39-SET/pb451-self-sender-factory-method-instance-receiver (SR12 c)3.),
      *> …-self-sender-instance-method-factory-receiver (SR12 c)4.),
      *> …-active-class-receiver-in-factory-method (SR14 b)1.),
      *> …-active-class-factory-receiver-in-instance-method (SR14 b)2.),
      *> …-self-sender-factory-not-implements (SR10 d)1.).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB451S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB451P
           CLASS PB451Q.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OP USAGE OBJECT REFERENCE PB451P.
       01 OQ USAGE OBJECT REFERENCE PB451Q.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB451P "FMK".
           INVOKE PB451Q "QFMK".
           INVOKE PB451P "NEW" RETURNING OP.
           INVOKE OP "MK".
           INVOKE PB451Q "NEW" RETURNING OQ.
           INVOKE OQ "QMK".
           SET OQ TO NULL.
           IF OQ = NULL
               DISPLAY "NULLOK"
           END-IF.
           STOP RUN.
       END PROGRAM PB451S.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB451J.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE PB451J.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB451K INHERITS FROM PB451J.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB451J.
       PROCEDURE DIVISION.
       END INTERFACE PB451K.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451P INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE PB451J
           INTERFACE PB451K.
       IDENTIFICATION DIVISION.
       FACTORY. IMPLEMENTS PB451K.
       PROCEDURE DIVISION.
       METHOD-ID. TAG.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FTAG-P".
       END METHOD TAG.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FPING-P".
       END METHOD PING.
       METHOD-ID. FMK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 FR USAGE OBJECT REFERENCE FACTORY OF PB451P.
       01 JR USAGE OBJECT REFERENCE PB451J.
       PROCEDURE DIVISION.
       MAIN.
           SET FR TO SELF.
           INVOKE FR "TAG".
           SET JR TO SELF.
           INVOKE JR "PING".
       END METHOD FMK.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS PB451K.
       PROCEDURE DIVISION.
       METHOD-ID. WHO.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "WHO-P".
       END METHOD WHO.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "IPING-P".
       END METHOD PING.
       METHOD-ID. MK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 O2 USAGE OBJECT REFERENCE PB451P.
       01 J2 USAGE OBJECT REFERENCE PB451J.
       PROCEDURE DIVISION.
       MAIN.
           SET O2 TO SELF.
           INVOKE O2 "WHO".
           SET J2 TO SELF.
           INVOKE J2 "PING".
       END METHOD MK.
       END OBJECT.
       END CLASS PB451P.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451Q INHERITS FROM PB451P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB451P
           INTERFACE PB451J.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. PING OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FPING-Q".
       END METHOD PING.
       METHOD-ID. QFMK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 JR USAGE OBJECT REFERENCE PB451J.
       PROCEDURE DIVISION.
       MAIN.
           SET JR TO SELF.
           INVOKE JR "PING".
       END METHOD QFMK.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WHO OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "WHO-Q".
       END METHOD WHO.
       METHOD-ID. PING OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "IPING-Q".
       END METHOD PING.
       METHOD-ID. QMK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 PR USAGE OBJECT REFERENCE PB451P.
       01 J2 USAGE OBJECT REFERENCE PB451J.
       PROCEDURE DIVISION.
       MAIN.
           SET PR TO SELF.
           INVOKE PR "WHO".
           SET J2 TO SELF.
           INVOKE J2 "PING".
       END METHOD QMK.
       END OBJECT.
       END CLASS PB451Q.
