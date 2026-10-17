      *>--- conformance-case
      *> id: l1c05-oo-returning-only-subclass
      *> rules: [GR-9.3.8.2.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §9.3.8.2.3 rule 5 c) 1. — prototype returns X ONLY,
      *>   implementation Y ONLY (a subclass)
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
      *> Interface-2 = L1C05TI (GETX RETURNING OBJECT REFERENCE L1C05TX
      *>   ONLY); interface-1 = the object interface of L1C05TC, whose
      *>   GETX returns OBJECT REFERENCE L1C05TY ONLY where L1C05TY
      *>   INHERITS FROM L1C05TX: ONLY is present but the
      *>   object-class-name is not "the same" (rule 5 c) 2., which
      *>   admits a subclass, applies only WITHOUT ONLY), so it does not
      *>   conform and §11.8.3 SR2 is violated: COBOLNET0841.
       IDENTIFICATION DIVISION.
       CLASS-ID. L1C05TX INHERITS FROM BASE.
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
           DISPLAY "SPEAK-L1C05TX".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS L1C05TX.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C05TY INHERITS FROM L1C05TX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C05TX.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "SPEAK-L1C05TY".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS L1C05TY.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C05TI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C05TX.
       PROCEDURE DIVISION.
       METHOD-ID. GETX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R USAGE OBJECT REFERENCE L1C05TX ONLY.
       PROCEDURE DIVISION RETURNING LK-R.
       END METHOD GETX.
       END INTERFACE L1C05TI.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C05TC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C05TX
           CLASS L1C05TY
           INTERFACE L1C05TI.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS L1C05TI.
       PROCEDURE DIVISION.
       METHOD-ID. GETX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R USAGE OBJECT REFERENCE L1C05TY ONLY.
       PROCEDURE DIVISION RETURNING LK-R.
       MAIN.
           INVOKE L1C05TY "NEW" RETURNING LK-R.
       END METHOD GETX.
       END OBJECT.
       END CLASS L1C05TC.
