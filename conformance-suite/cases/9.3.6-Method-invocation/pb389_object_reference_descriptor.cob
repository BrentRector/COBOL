*>--- conformance-case
*> id: pb389_object_reference_descriptor
*> rules: [GR-9.3.6-1, GR-9.3.6-L2.2, SR-13.18.60.3-16,
*>    GR-13.18.60.4-22, SR-14.9.39.3-10, SR-14.9.39.3-12,
*>    SR-14.9.39.3-13, SR-14.9.39.3-14, GR-14.9.39.4-9, GR-14.9.39.4-10]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.60.2 — the USAGE OBJECT REFERENCE general format is a TUPLE over four
      *> INDEPENDENT axes: kind (universal | interface-name-1 | [FACTORY OF] ACTIVE-CLASS |
      *> [FACTORY OF] object-class-name-1 [ONLY]) × FACTORY × ONLY × the name.  This program
      *> writes FOUR of those shapes and exercises the §14.9.39.3 SET format-5 rules that
      *> DISCRIMINATE on the axes a single class-name could not express.  kb/Work PB389.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES — NOT FROM A RUN:
      *>
      *> 1. INVOKE PB389B "NEW" RETURNING B-ONLY.  §16.2.1.2 GR1: "The New method allocates
      *>    storage for an object … and returns a reference to the created object" — an
      *>    INSTANCE object of exactly PB389B.  The receiver is described ONLY, and
      *>    §13.18.60.4 GR22 d)2.b. makes that "an instance object of the specified class";
      *>    the delivery follows the SET rules (§14.8.3.3 rule 1) and satisfies §14.9.39.3
      *>    SR12 a)1.  INVOKE B-ONLY "WHO" then resolves on the OBJECT (§14.9.23.4 GR2 —
      *>    "identifier-1 identifies an instance object … a method OF THAT OBJECT"), so the
      *>    line is  BASE.
      *>
      *> 2. SET B TO D — SR12 a)2., the subclass leg (neither side carries ONLY or FACTORY,
      *>    so a)1. and a)3. are satisfied by absence).  INVOKE B "MK" runs PB389B's MK on a
      *>    PB389D object.  MK's LOCAL-STORAGE item is described ACTIVE-CLASS, which
      *>    §13.18.60.4 GR22 e) binds to "the same class as the object that was used to
      *>    invoke the method in which this data description entry is specified" — PB389D.
      *>    SET A TO SELF is SR14 b)1. (no FACTORY phrase, an instance definition), and
      *>    INVOKE A "WHO" again resolves on the object, so the line is  DERIVED.
      *>    DISCRIMINATOR: an ACTIVE-CLASS item bound statically to its CONTAINING class
      *>    would print BASE here.
      *>
      *> 3. SET F TO PB389D — SR13: identifier-3 is described with an object-class-name, so
      *>    "the data item shall be described with the FACTORY phrase" (it is), and b)
      *>    "object-class-name-1 shall reference the same class or a subclass" (PB389D is a
      *>    subclass of PB389B).  F now holds PB389D's FACTORY object, which GR22 d)1.a.
      *>    expressly permits ("the factory object of the specified class or of a subclass").
      *>    INVOKE F "TAG" resolves the FACTORY method interface — §9.3.6 gives a class two
      *>    separate interfaces — so the line is  FTAG-D.
      *>    DISCRIMINATOR: resolving the INSTANCE roster would not find TAG at all; copying
      *>    the base's factory object would print FTAG-B.
      *>
      *> 3b. SET B B2 TO D is the MULTI-RECEIVER form §14.9.39.4 GR9 states: "a reference to
      *>    the object identified by identifier-4 is placed into EACH data item referenced by
      *>    identifier-3 in the order specified".  Both receivers then answer WHO with the
      *>    class of the object D holds — DERIVED twice.  DISCRIMINATOR: placing the
      *>    reference into only the first receiver leaves B2 null and raises EC-OO-NULL.
      *>
      *> 3c. Inside MK, SET B3 TO A is SR12 b): identifier-3 is described with an
      *>    object-class-name and identifier-4 with an ACTIVE-CLASS phrase, so b)1. (the
      *>    receiver carries no ONLY phrase), b)2. ("the class containing the data item
      *>    referenced by identifier-4 shall be the same class or a subclass" — A is written
      *>    in PB389B and the receiver is PB389B) and b)3. (neither carries FACTORY) all
      *>    hold.  SET IR TO A is SR10 c)2.: an interface-name receiver with an ACTIVE-CLASS
      *>    sender that carries no FACTORY phrase, so "the objects of the class containing
      *>    the data item referenced by identifier-4 shall implement int-1" — PB389B's
      *>    instance definition carries IMPLEMENTS PB389I.  INVOKE IR "PING" dispatches on
      *>    the object held, a PB389D instance, so the line is  IPING-D.
      *>
      *> 3d. The factory method FMK is the FACTORY-side twin: FA is described FACTORY OF
      *>    ACTIVE-CLASS, SET FA TO SELF is SR14 b)2. (the receiver carries FACTORY, and the
      *>    method IS in the factory definition), and SET FIR TO FA is SR10 c)1. — an
      *>    ACTIVE-CLASS sender WITH a FACTORY phrase, so "the factory object of the class
      *>    containing the data item referenced by identifier-4 shall implement int-1", which
      *>    PB389B's FACTORY IMPLEMENTS clause supplies.  Invoked on PB389D, the factory
      *>    object held is PB389D's, so the line is  PING-D.
      *>
      *> 3e. SET R TO B is SR10 b)2. — an interface-name receiver taking an object reference
      *>    described with an object-class-name and WITHOUT a FACTORY phrase, so "the objects
      *>    of the specified class shall implement int-1": PB389B's instance definition
      *>    carries IMPLEMENTS PB389I.  B holds the PB389D instance, so INVOKE R "PING"
      *>    prints  IPING-D.
      *>
      *> 4. SET R TO F — SR10 b)1.: the receiver is described with an interface-name, the
      *>    sender with an object-class-name AND a FACTORY phrase, so "the factory object of
      *>    the specified class shall implement int-1".  PB389B's FACTORY carries
      *>    IMPLEMENTS PB389I, so it does.  INVOKE R "PING" resolves over the interface
      *>    (§14.9.23.3 SR4e) and dispatches on the object actually held — PB389D's factory —
      *>    so the line is  PING-D.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB389M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB389I
           CLASS PB389B
           CLASS PB389D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B-ONLY USAGE OBJECT REFERENCE PB389B ONLY.
       01 B       USAGE OBJECT REFERENCE PB389B.
       01 B2      USAGE OBJECT REFERENCE PB389B.
       01 D       USAGE OBJECT REFERENCE PB389D.
       01 F       USAGE OBJECT REFERENCE FACTORY OF PB389B.
       01 R       USAGE OBJECT REFERENCE PB389I.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB389B "NEW" RETURNING B-ONLY.
           INVOKE B-ONLY "WHO".
           INVOKE PB389D "NEW" RETURNING D.
           SET B B2 TO D.
           INVOKE B "WHO".
           INVOKE B2 "WHO".
           INVOKE B "MK".
           INVOKE PB389D "FMK".
           SET R TO B.
           INVOKE R "PING".
           SET F TO PB389D.
           INVOKE F "TAG".
           SET R TO F.
           INVOKE R "PING".
           STOP RUN.
       END PROGRAM PB389M.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB389I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE PB389I.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB389B INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE PB389I.
       IDENTIFICATION DIVISION.
       FACTORY. IMPLEMENTS PB389I.
       PROCEDURE DIVISION.
       METHOD-ID. TAG.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FTAG-B".
       END METHOD TAG.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "PING-B".
       END METHOD PING.
       METHOD-ID. FMK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 FA  USAGE OBJECT REFERENCE FACTORY OF ACTIVE-CLASS.
       01 FIR USAGE OBJECT REFERENCE PB389I.
       PROCEDURE DIVISION.
       MAIN.
           SET FA TO SELF.
           SET FIR TO FA.
           INVOKE FIR "PING".
       END METHOD FMK.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS PB389I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "IPING-B".
       END METHOD PING.
       METHOD-ID. WHO.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "BASE".
       END METHOD WHO.
       METHOD-ID. MK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 A  USAGE OBJECT REFERENCE ACTIVE-CLASS.
       01 B3 USAGE OBJECT REFERENCE PB389B.
       01 IR USAGE OBJECT REFERENCE PB389I.
       PROCEDURE DIVISION.
       MAIN.
           SET A TO SELF.
           INVOKE A "WHO".
           SET B3 TO A.
           SET IR TO A.
           INVOKE IR "PING".
       END METHOD MK.
       END OBJECT.
       END CLASS PB389B.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB389D INHERITS FROM PB389B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB389I
           CLASS PB389B.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. TAG OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FTAG-D".
       END METHOD TAG.
       METHOD-ID. PING OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "PING-D".
       END METHOD PING.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS PB389I.
       PROCEDURE DIVISION.
       METHOD-ID. PING OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "IPING-D".
       END METHOD PING.
       METHOD-ID. WHO OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "DERIVED".
       END METHOD WHO.
       END OBJECT.
       END CLASS PB389D.
