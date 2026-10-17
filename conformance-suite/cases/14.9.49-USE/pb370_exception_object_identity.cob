      *>--- conformance-case
      *> id: pb370_exception_object_identity
      *> rules: [GR-14.9.49.4-15]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.49.4 GR15: "Upon entry to the associated declarative,
      *> the predefined object reference EXCEPTION-OBJECT references the
      *> exception object." That is an IDENTITY claim, and §8.8.4.2.15
      *> makes object-reference equality one: the relation is true "if
      *> the object referenced by identifier-3 is the same object that
      *> is referenced by identifier-4". So each leg compares the
      *> register with the very object RAISEd AND with a second live
      *> instance of the same class: a register set to SELF, to a fresh
      *> instance, to the first object ever raised or to any other
      *> non-null object fails one of the two comparisons (a NOT = NULL
      *> test, which conformance:2002/oo_ec_raise_object makes, passes
      *> all of them).
      *>   P1  RAISE E1 in the program: EQ-E1 and NE-E2.
      *>   P2  RAISE E2 next: NE-E1 and EQ-E2 - the register follows the
      *>       raise, it is not latched by the first one.
      *>   M1  a METHOD's own declarative (§14.2.2 SR10 admits the
      *>       DECLARATIVES format in a method definition): RAISE M1 in
      *>       the method enters the method's USE, where
      *>       EXCEPTION-OBJECT is M1 and not M2. GR14 a) selects the
      *>       object-class-name-1 arm (the raised object is an instance
      *>       of PB370ER).
      *> §14.6.13.1.5: "If execution of the declarative completes
      *> normally, execution continues with the statement following the
      *> RAISE statement."
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB370EO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB370ER
           CLASS PB370HO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 USAGE OBJECT REFERENCE PB370ER.
       01 E2 USAGE OBJECT REFERENCE PB370ER.
       01 U  USAGE OBJECT REFERENCE.
       01 H  USAGE OBJECT REFERENCE PB370HO.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-ERR SECTION.
           USE AFTER EXCEPTION OBJECT PB370ER.
       D-ERR-P.
           SET U TO EXCEPTION-OBJECT
           IF U = E1 DISPLAY "EQ-E1" ELSE DISPLAY "NE-E1" END-IF
           IF U = E2 DISPLAY "EQ-E2" ELSE DISPLAY "NE-E2" END-IF.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE PB370ER "NEW" RETURNING E1
           INVOKE PB370ER "NEW" RETURNING E2
           DISPLAY "P1"
           RAISE E1
           DISPLAY "P2"
           RAISE E2
           DISPLAY "M1"
           INVOKE PB370HO "NEW" RETURNING H
           INVOKE H "RAISE-ONE"
           DISPLAY "END"
           STOP RUN.
       END PROGRAM PB370EO.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB370ER INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS PB370ER.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB370HO INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB370ER.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. RAISE-ONE.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 M1 USAGE OBJECT REFERENCE PB370ER.
       01 M2 USAGE OBJECT REFERENCE PB370ER.
       01 MU USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       M-ERR SECTION.
           USE AFTER EXCEPTION OBJECT PB370ER.
       M-ERR-P.
           SET MU TO EXCEPTION-OBJECT
           IF MU = M1 DISPLAY "M-EQ-M1" ELSE DISPLAY "M-NE-M1" END-IF
           IF MU = M2 DISPLAY "M-EQ-M2" ELSE DISPLAY "M-NE-M2" END-IF.
       END DECLARATIVES.
       M-MAIN SECTION.
       M-MAIN-P.
           INVOKE PB370ER "NEW" RETURNING M1
           INVOKE PB370ER "NEW" RETURNING M2
           RAISE M1
           DISPLAY "M-AFTER".
       END METHOD RAISE-ONE.
       END OBJECT.
       END CLASS PB370HO.
