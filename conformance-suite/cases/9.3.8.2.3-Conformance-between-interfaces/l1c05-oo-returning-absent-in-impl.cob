      *>--- conformance-case
      *> id: l1c05-oo-returning-absent-in-impl
      *> rules: [GR-9.3.8.2.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §9.3.8.2.3 rule 4 — RETURNING presence differs: the
      *>   prototype has RETURNING and the implementation has none
      *> Rule: "The presence or absence of the procedure division
      *>   RETURNING phrase is the same."
      *> cite.py --check 9.3.8.2.3 "The presence or absence of the
      *>   procedure division RETURNING phrase is the same" -> OK
      *>   §9.3.8.2.3 4)  (Conformance between interfaces)
      *> cite.py --check 11.8.3 "Each method prototype in each
      *>   implemented interface shall be such that the object interface
      *>   of this class conforms to all implemented interfaces" -> OK
      *>   §11.8.3 2)  (Syntax rules)
      *> Interface-1 = the object interface of class L1C05PC,
      *>   interface-2 = L1C05PI (the class IMPLEMENTS it). Method ADDV
      *>   is identical on both sides (one BY REFERENCE PIC 9(4) formal)
      *>   EXCEPT that the prototype has RETURNING and the
      *>   implementation has none, so the object interface does not
      *>   conform and §11.8.3 SR2 is violated: COBOLNET0841 (the
      *>   IMPLEMENTS conformance diagnostic). Control: with RETURNING
      *>   LK-R on both sides the same source compiles.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C05PI.
       PROCEDURE DIVISION.
       METHOD-ID. ADDV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V PIC 9(4).
       01 LK-R PIC 9(4).
       PROCEDURE DIVISION USING LK-V RETURNING LK-R.
       END METHOD ADDV.
       END INTERFACE L1C05PI.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C05PC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE L1C05PI.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS L1C05PI.
       PROCEDURE DIVISION.
       METHOD-ID. ADDV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V PIC 9(4).
       01 LK-R PIC 9(4).
       PROCEDURE DIVISION USING LK-V.
       MAIN.
           CONTINUE.
       END METHOD ADDV.
       END OBJECT.
       END CLASS L1C05PC.
