*>--- conformance-case
*> id: pb496_active_class_positions
*> rules: [SR-13.18.60.3-16, GR-13.18.60.4-22, SR-14.9.39.3-14]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.60.3 SR16 — "The ACTIVE-CLASS phrase may be specified only in a factory
      *> definition, an instance definition, or the linkage or local-storage section of a
      *> method definition."  FOUR permitted positions; conformance:2002/
      *> pb389_object_reference_descriptor writes the fourth (a method's LOCAL-STORAGE), and
      *> conformance:cases/13.18.60-USAGE/pb389-active-class-outside-class and
      *> …-active-class-method-working-storage pin two of the forbidden ones.  This program
      *> writes THE OTHER THREE and makes each one OBSERVABLE, so SR16's permission is proved
      *> by a running program rather than by the absence of a diagnostic.  kb/Work PB496.
      *>
      *> Each declaration is also a §13.18.60.4 GR22 e) witness: "If ACTIVE-CLASS is
      *> specified, the object referenced by this data item shall be of the same class as the
      *> object that was used to invoke the method in which this data description entry is
      *> specified" — e)1. the factory object of that class when FACTORY is written, e)2. an
      *> instance object of it when it is not.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES — NOT FROM A RUN:
      *>
      *> 1-2. FAW is declared in the FACTORY definition's WORKING-STORAGE (SR16 position 1)
      *>    as FACTORY OF ACTIVE-CLASS.  FMK is a factory method; SET FAW TO SELF is
      *>    §14.9.39.3 SR14 b)2. (the receiver carries the FACTORY phrase and the method IS
      *>    defined in the factory definition).  GR22 e)1. then binds FAW to the factory
      *>    object of the class that invoked the method, so INVOKE FAW "TAG" prints  FTAG-C
      *>    when invoked on PB496C and                                               FTAG-D
      *>    when the SAME inherited method is invoked on PB496D.
      *>    DISCRIMINATOR: an ACTIVE-CLASS item bound statically to its CONTAINING class
      *>    prints FTAG-C both times.
      *>
      *> 3-4. AW is declared in the INSTANCE definition's WORKING-STORAGE (SR16 position 2)
      *>    as a plain ACTIVE-CLASS reference.  MK is an instance method; SET AW TO SELF is
      *>    SR14 b)1., and GR22 e)2. binds AW to an instance object of the invoking object's
      *>    class.  INVOKE AW "WHO" prints                                            WHO-C
      *>    then, on a PB496D object,                                                 WHO-D.
      *>
      *> 5-6. P is declared in a method's LINKAGE SECTION (SR16 position 3) as ACTIVE-CLASS
      *>    and received BY REFERENCE through PROCEDURE DIVISION USING.  MK passes AW to it,
      *>    an ACTIVE-CLASS argument into an ACTIVE-CLASS formal, and TAKE invokes WHO on it,
      *>    so each MK prints its class's WHO twice — WHO-C WHO-C, then WHO-D WHO-D.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB496M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB496C
           CLASS PB496D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB496C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB496C "FMK".
           INVOKE PB496D "FMK".
           INVOKE PB496C "NEW" RETURNING O.
           INVOKE O "MK".
           INVOKE PB496D "NEW" RETURNING O.
           INVOKE O "MK".
           STOP RUN.
       END PROGRAM PB496M.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB496C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FAW USAGE OBJECT REFERENCE FACTORY OF ACTIVE-CLASS.
       PROCEDURE DIVISION.
       METHOD-ID. TAG.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FTAG-C".
       END METHOD TAG.
       METHOD-ID. FMK.
       PROCEDURE DIVISION.
       MAIN.
           SET FAW TO SELF.
           INVOKE FAW "TAG".
       END METHOD FMK.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AW USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION.
       METHOD-ID. WHO.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "WHO-C".
       END METHOD WHO.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION USING P.
       MAIN.
           INVOKE P "WHO".
       END METHOD TAKE.
       METHOD-ID. MK.
       PROCEDURE DIVISION.
       MAIN.
           SET AW TO SELF.
           INVOKE AW "WHO".
           INVOKE SELF "TAKE" USING AW.
       END METHOD MK.
       END OBJECT.
       END CLASS PB496C.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB496D INHERITS FROM PB496C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB496C.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. TAG OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FTAG-D".
       END METHOD TAG.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WHO OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "WHO-D".
       END METHOD WHO.
       END OBJECT.
       END CLASS PB496D.
