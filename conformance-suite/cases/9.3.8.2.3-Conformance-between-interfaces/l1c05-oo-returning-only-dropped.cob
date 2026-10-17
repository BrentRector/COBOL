      *>--- conformance-case
      *> id: l1c05-oo-returning-only-dropped
      *> rules: [GR-9.3.8.2.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §9.3.8.2.3 rule 5 c) 1. — prototype returns X ONLY,
      *>   implementation drops ONLY
      *> Rule: "If the returning item in interface-2 is described with
      *>   the ONLY phrase, the returning item in interface-1 shall be
      *>   described with the ONLY phrase and the same
      *>   object-class-name."
      *> cite.py --check 9.3.8.2.3 "If the returning item in interface-2
      *>   is described with the ONLY phrase, the returning item in
      *>   interface-1 shall be described with the ONLY phrase and the
      *>   same object-class-name" -> OK  §9.3.8.2.3 5) 2. c)
      *>   (Conformance between interfaces)
      *> cite.py --check 11.8.3 "Each method prototype in each
      *>   implemented interface shall be such that the object interface
      *>   of this class conforms to all implemented interfaces" -> OK
      *>   §11.8.3 2)  (Syntax rules)
      *> Interface-2 = L1C05SI (GETX RETURNING OBJECT REFERENCE L1C05SX
      *>   ONLY); interface-1 = the object interface of L1C05SC, whose
      *>   GETX returns OBJECT REFERENCE L1C05SX WITHOUT ONLY: the same
      *>   class, but not "described with the ONLY phrase", so it does
      *>   not conform and §11.8.3 SR2 is violated: COBOLNET0841.
      *>   Everything else is identical to the admitted
      *>   conformance:2002/l1c05_oo_returning_only_same_class.
       IDENTIFICATION DIVISION.
       CLASS-ID. L1C05SX INHERITS FROM BASE.
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
           DISPLAY "SPEAK-L1C05SX".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS L1C05SX.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C05SY INHERITS FROM L1C05SX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C05SX.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "SPEAK-L1C05SY".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS L1C05SY.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C05SI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C05SX.
       PROCEDURE DIVISION.
       METHOD-ID. GETX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R USAGE OBJECT REFERENCE L1C05SX ONLY.
       PROCEDURE DIVISION RETURNING LK-R.
       END METHOD GETX.
       END INTERFACE L1C05SI.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C05SC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C05SX
           CLASS L1C05SY
           INTERFACE L1C05SI.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS L1C05SI.
       PROCEDURE DIVISION.
       METHOD-ID. GETX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R USAGE OBJECT REFERENCE L1C05SX.
       PROCEDURE DIVISION RETURNING LK-R.
       MAIN.
           INVOKE L1C05SX "NEW" RETURNING LK-R.
       END METHOD GETX.
       END OBJECT.
       END CLASS L1C05SC.
