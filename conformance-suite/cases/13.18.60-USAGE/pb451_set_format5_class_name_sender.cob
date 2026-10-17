*>--- conformance-case
*> id: pb451_set_format5_class_name_sender
*> rules: [GR-13.18.60.4-22, SR-14.9.39.3-11, SR-14.9.39.3-13, GR-14.9.39.4-10]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 / §14.9.39.4 — SET format 5 with object-class-name-1 as the SENDER.
      *> The sender is a class NAME, not identifier-4, and §14.9.39.4 GR10 says what it sends:
      *> "If object-class-name-1 is specified, a reference to the factory object of the class
      *> identified by object-class-name-1 is placed into each data item referenced by
      *> identifier-3 in the order specified."  WHICH SYNTAX RULE THEN GOVERNS IS READ OFF THE
      *> RECEIVER: SR11 when identifier-3 is described with an interface-name, SR13 when it is
      *> described with an object-class-name.  kb/Work PB451 + PB496.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES — NOT FROM A RUN:
      *>
      *> 1. SET IR TO PB451B — SR11: "If object-class-name-1 is specified and the data item
      *>    referenced by identifier-3 is described with an interface-name that identifies the
      *>    interface int-1, the factory object of object-class-name-1 shall be described with
      *>    an IMPLEMENTS clause that references int-1."  PB451B's FACTORY carries
      *>    IMPLEMENTS PB451I, so the assignment is legal, and GR10 puts PB451B's FACTORY
      *>    OBJECT — not an instance — into IR.  §9.3.6 gives a class two separate method
      *>    interfaces, so INVOKE IR "PING" resolves on the FACTORY roster:   PING-B
      *>    DISCRIMINATOR: resolving the INSTANCE roster would print IPING-B.
      *>
      *> 2. SET IR TO PB451D — the same rule over the INHERITED leg.  PB451D's FACTORY carries
      *>    NO IMPLEMENTS clause of its own; §11.4.4 GR2 c) makes its factory object implement
      *>    PB451I anyway ("the class containing the factory object inherits a class whose
      *>    factory object implements int-1"), which is the relation §11.4.4 GR1 says the
      *>    IMPLEMENTS clause names ("the interfaces that are implemented … according to
      *>    9.3.11").  PB451D's FACTORY overrides PING, so the line is:                PING-D
      *>    DISCRIMINATOR: reading SR11 as a literal claim about the clause TEXT would refuse
      *>    this program outright; copying the base's factory object would print PING-B.
      *>
      *> 3. SET FO TO PB451B — SR13's leading requirement, "the data item shall be described
      *>    with the FACTORY phrase" (FO is), and SR13 a): "if the data item referenced by
      *>    identifier-3 is described with the ONLY phrase, object-class-name-1 shall be the
      *>    object-class-name specified in the description of the data item referenced by
      *>    identifier-3."  FO is described FACTORY OF PB451B ONLY and the sender names
      *>    PB451B exactly, so it holds what §13.18.60.4 GR22 d)2.a. allows an ONLY factory
      *>    reference to hold — "the factory object of the specified class", that class and no
      *>    subclass.  INVOKE FO "TAG" is therefore:                                  FTAG-B
      *>    DISCRIMINATOR: the same receiver with PB451D as the sender is refused — fixture
      *>    conformance:negative/pb451-classname-sender-only-receiver-subclass.
      *>
      *> 4. SET F1 F2 TO PB451D — GR10's multi-receiver form, "placed into EACH data item
      *>    referenced by identifier-3 in the order specified", over receivers described
      *>    FACTORY OF PB451B WITHOUT the ONLY phrase, which §13.18.60.4 GR22 d)1.a. lets hold
      *>    "the factory object of the specified class OR OF A SUBCLASS".  SR13 b) permits the
      *>    subclass name.  Both receivers answer TAG with PB451D's override:
      *>                                                                       FTAG-D  FTAG-D
      *>    DISCRIMINATOR: placing the reference into only the first receiver leaves F2 null
      *>    and raises EC-OO-NULL; ignoring the subclass would print FTAG-B twice.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB451M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB451I
           CLASS PB451B
           CLASS PB451D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IR USAGE OBJECT REFERENCE PB451I.
       01 FO USAGE OBJECT REFERENCE FACTORY OF PB451B ONLY.
       01 F1 USAGE OBJECT REFERENCE FACTORY OF PB451B.
       01 F2 USAGE OBJECT REFERENCE FACTORY OF PB451B.
       PROCEDURE DIVISION.
       MAIN.
           SET IR TO PB451B.
           INVOKE IR "PING".
           SET IR TO PB451D.
           INVOKE IR "PING".
           SET FO TO PB451B.
           INVOKE FO "TAG".
           SET F1 F2 TO PB451D.
           INVOKE F1 "TAG".
           INVOKE F2 "TAG".
           STOP RUN.
       END PROGRAM PB451M.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB451I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE PB451I.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB451I.
       IDENTIFICATION DIVISION.
       FACTORY. IMPLEMENTS PB451I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "PING-B".
       END METHOD PING.
       METHOD-ID. TAG.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FTAG-B".
       END METHOD TAG.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "IPING-B".
       END METHOD PING.
       END OBJECT.
       END CLASS PB451B.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451D INHERITS FROM PB451B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB451B
           INTERFACE PB451I.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. PING OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "PING-D".
       END METHOD PING.
       METHOD-ID. TAG OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FTAG-D".
       END METHOD TAG.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PING OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "IPING-D".
       END METHOD PING.
       END OBJECT.
       END CLASS PB451D.
