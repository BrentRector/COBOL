      *>--- conformance-case
      *> id: l1c05-oo-strong-type-vs-plain-group
      *> rules: [GR-9.3.8.2.3-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §9.3.8.2.3 rule 7 — the formal is a plain (not strongly
      *>   typed) group of the same width
      *> Rule: "If either of the corresponding formal parameters or
      *>   returning items in interface-1 or interface-2 is a
      *>   strongly-typed group item, both are of the same type."
      *> cite.py --check 9.3.8.2.3 "If either of the corresponding
      *>   formal parameters or returning items in interface-1 or
      *>   interface-2 is a strongly-typed group item, both are of the
      *>   same type" -> OK  §9.3.8.2.3 7)  (Conformance between
      *>   interfaces)
      *> cite.py --check 8.5.3.1 "Two type declarations are considered
      *>   equivalent when they have the same type-name, both have the
      *>   same presence or absence of the EXTERNAL clause and the
      *>   STRONG phrase" -> OK  §8.5.3.1   (General)
      *> cite.py --check 11.8.3 "Each method prototype in each
      *>   implemented interface shall be such that the object interface
      *>   of this class conforms to all implemented interfaces" -> OK
      *>   §11.8.3 2)  (Syntax rules)
      *> Interface-2 = L1C05WI (SWAPT USING LK-P RETURNING LK-R, both
      *>   TYPE TT, TT a STRONG group of one PIC X(4)); interface-1 =
      *>   the object interface of L1C05WC, which IMPLEMENTS L1C05WI.
      *>   Everything else matches
      *>   conformance:cases/9.3.8.2.3-Conformance-between-interfaces/l1c05_oo_strong_type_same_type; only the
      *>   item named above is not "of the same type" (§8.5.3.1), so the
      *>   object interface does not conform and §11.8.3 SR2 is
      *>   violated: COBOLNET0841.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C05WI.
       PROCEDURE DIVISION.
       METHOD-ID. SWAPT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TT TYPEDEF STRONG.
          05 TT-F PIC X(4).
       01 LK-P TYPE TT.
       01 LK-R TYPE TT.
       PROCEDURE DIVISION USING LK-P RETURNING LK-R.
       END METHOD SWAPT.
       END INTERFACE L1C05WI.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C05WC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE L1C05WI.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS L1C05WI.
       PROCEDURE DIVISION.
       METHOD-ID. SWAPT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TT TYPEDEF STRONG.
          05 TT-F PIC X(4).
       01 LK-P.
          05 LK-P-F PIC X(4).
       01 LK-R TYPE TT.
       PROCEDURE DIVISION USING LK-P RETURNING LK-R.
       MAIN.
           CONTINUE.
       END METHOD SWAPT.
       END OBJECT.
       END CLASS L1C05WC.
