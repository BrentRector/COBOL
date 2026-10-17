      *>--- conformance-case
      *> id: l1c05_oo_returning_only_same_class
      *> rules: [GR-9.3.8.2.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §9.3.8.2.3 rule 5 c) 1. — prototype returns X ONLY;
      *>   implementation X ONLY conforms
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
      *> The admitting arm: interface-2 = the interface L1C05RI, whose
      *>   GETX returns OBJECT REFERENCE L1C05RX ONLY; interface-1 = the
      *>   object interface of L1C05RC, which IMPLEMENTS L1C05RI and
      *>   whose GETX returns OBJECT REFERENCE L1C05RX ONLY: the ONLY
      *>   phrase and the same object-class-name, so interface-1
      *>   conforms and the program compiles. The rejecting arms are
      *>   negative/l1c05-oo-returning-only-dropped (no ONLY) and
      *>   negative/l1c05-oo-returning-only-subclass (a subclass ONLY).
      *> Derivation: GETX returns a new L1C05RX instance; SPEAK on it ->
      *>   "SPEAK-L1C05RX".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C05R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C05RC
           CLASS L1C05RX
           INTERFACE L1C05RI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-C USAGE OBJECT REFERENCE L1C05RI.
       01 W-X USAGE OBJECT REFERENCE L1C05RX ONLY.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE L1C05RC "NEW" RETURNING W-C.
           INVOKE W-C "GETX" RETURNING W-X.
           INVOKE W-X "SPEAK".
           STOP RUN.
       END PROGRAM L1C05R.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C05RX INHERITS FROM BASE.
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
           DISPLAY "SPEAK-L1C05RX".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS L1C05RX.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C05RY INHERITS FROM L1C05RX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C05RX.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "SPEAK-L1C05RY".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS L1C05RY.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C05RI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C05RX.
       PROCEDURE DIVISION.
       METHOD-ID. GETX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R USAGE OBJECT REFERENCE L1C05RX ONLY.
       PROCEDURE DIVISION RETURNING LK-R.
       END METHOD GETX.
       END INTERFACE L1C05RI.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C05RC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS L1C05RX
           CLASS L1C05RY
           INTERFACE L1C05RI.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS L1C05RI.
       PROCEDURE DIVISION.
       METHOD-ID. GETX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R USAGE OBJECT REFERENCE L1C05RX ONLY.
       PROCEDURE DIVISION RETURNING LK-R.
       MAIN.
           INVOKE L1C05RX "NEW" RETURNING LK-R.
       END METHOD GETX.
       END OBJECT.
       END CLASS L1C05RC.
