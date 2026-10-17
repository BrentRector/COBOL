      *>--- conformance-case
      *> id: l1c24_repository_self_interface_specifier
      *> rules: [SR-12.3.8.3-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.3.8.3 SR8 — an interface-specifier naming its own
      *> interface definition is ignored
      *> RULE §12.3.8.3 SR8: "If the specified interface-name-2 is the
      *>   name of the interface definition in which this REPOSITORY
      *>   paragraph is specified, references to interface-name-2 are
      *>   to that interface definition and this interface-specifier is
      *>   ignored."
      *>   cite.py --check 12.3.8.3 "If the specified interface-name-2
      *>   is the name of the interface definition in which this
      *>   REPOSITORY paragraph is specified" -> OK §12.3.8.3 8)
      *>   cite.py --check 12.3.8.4 "literal-1, literal-2, literal-3,
      *>   or literal-5 is the externalized name by which the class,
      *>   interface, function, or program, respectively, is known to
      *>   the operating environment" -> OK §12.3.8.4 2)
      *> SET-UP: interface L1C24N's own REPOSITORY writes INTERFACE
      *>   L1C24N AS "L1C24O"; L1C24O is a real interface with a
      *>   DIFFERENT method set (ONLYO only), so L1C24N does not conform
      *>   to it. L1C24N's prototype PEER takes an OBJECT REFERENCE
      *>   L1C24N. By SR8 that type is L1C24N itself, so: the factory of
      *>   L1C24P implements PEER with a parameter of type L1C24N (a
      *>   matching signature), and main may pass its L1C24N reference.
      *>   Had the specifier been honoured, the prototype's parameter
      *>   would be L1C24O: the implementation would not match and the
      *>   argument would not conform - the program would not compile.
      *> EXPECTED OUTPUT, DERIVED:
      *>   PEER-CALLED         the factory method PEER runs;
      *>   HELLO-FROM-FACTORY  INVOKE LP "HELLO" inside it reaches the
      *>                       factory object main passed (R = F).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C24Q.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C24P
           INTERFACE L1C24N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE OBJECT REFERENCE FACTORY OF L1C24P.
       01 R USAGE OBJECT REFERENCE L1C24N.
       PROCEDURE DIVISION.
       MAIN-P.
           SET F TO L1C24P.
           SET R TO F.
           INVOKE R "PEER" USING R.
           STOP RUN.
       END PROGRAM L1C24Q.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C24O.
       PROCEDURE DIVISION.
       METHOD-ID. ONLYO.
       PROCEDURE DIVISION.
       END METHOD ONLYO.
       END INTERFACE L1C24O.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C24N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE L1C24N AS "L1C24O".
       PROCEDURE DIVISION.
       METHOD-ID. HELLO.
       PROCEDURE DIVISION.
       END METHOD HELLO.
       METHOD-ID. PEER.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP USAGE OBJECT REFERENCE L1C24N.
       PROCEDURE DIVISION USING LP.
       END METHOD PEER.
       END INTERFACE L1C24N.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C24P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE L1C24N.
       IDENTIFICATION DIVISION.
       FACTORY. IMPLEMENTS L1C24N.
       PROCEDURE DIVISION.
       METHOD-ID. HELLO.
       PROCEDURE DIVISION.
       HELLO-P.
           DISPLAY "HELLO-FROM-FACTORY".
       END METHOD HELLO.
       METHOD-ID. PEER.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP USAGE OBJECT REFERENCE L1C24N.
       PROCEDURE DIVISION USING LP.
       PEER-P.
           DISPLAY "PEER-CALLED".
           INVOKE LP "HELLO".
       END METHOD PEER.
       END FACTORY.
       END CLASS L1C24P.
