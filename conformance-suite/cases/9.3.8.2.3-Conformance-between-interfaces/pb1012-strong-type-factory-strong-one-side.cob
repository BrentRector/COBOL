      *>--- conformance-case
      *> id: pb1012-strong-type-factory-strong-one-side
      *> rules: [GR-9.3.8.2.3-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §9.3.8.2.3 rule 7 through §11.4.3 SR2 — a FACTORY that
      *>   IMPLEMENTS a prototype whose strong type it redeclares
      *>   WITHOUT the STRONG phrase
      *> Rule: "If either of the corresponding formal parameters or
      *>   returning items in interface-1 or interface-2 is a
      *>   strongly-typed group item, both are of the same type."
      *> cite.py --check 9.3.8.2.3 "If either of the corresponding
      *>   formal parameters or returning items in interface-1 or
      *>   interface-2 is a strongly-typed group item, both are of the
      *>   same type" -> OK  §9.3.8.2.3 7)  (Conformance between
      *>   interfaces)
      *> cite.py --check 8.5.3.1 "both have the same presence or
      *>   absence of the EXTERNAL clause and the STRONG phrase" -> OK
      *>   §8.5.3.1   (General)
      *> cite.py --check 11.4.3 "Each method prototype in each
      *>   implemented interface shall be such that the factory
      *>   interface of this class conforms to all implemented
      *>   interfaces" -> OK  §11.4.3 2)  (Syntax rules)
      *> Interface-2 = PB1012NI (SWAPT USING LK-P, TYPE TT, TT a
      *>   TYPEDEF STRONG group of one PIC X(4)); interface-1 = the
      *>   factory interface of PB1012NC, which IMPLEMENTS PB1012NI.
      *>   The factory method's TT has the same type-name and layout
      *>   but no STRONG phrase, so the two declarations are not
      *>   equivalent (§8.5.3.1): the prototype's formal is a
      *>   strongly-typed group item and the method's is not of its
      *>   type, so the factory interface does not conform and
      *>   §11.4.3 SR2 is violated: COBOLNET0841.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB1012NI.
       PROCEDURE DIVISION.
       METHOD-ID. SWAPT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TT TYPEDEF STRONG.
          05 TT-F PIC X(4).
       01 LK-P TYPE TT.
       PROCEDURE DIVISION USING LK-P.
       END METHOD SWAPT.
       END INTERFACE PB1012NI.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1012NC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB1012NI.
       IDENTIFICATION DIVISION.
       FACTORY. IMPLEMENTS PB1012NI.
       PROCEDURE DIVISION.
       METHOD-ID. SWAPT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TT TYPEDEF.
          05 TT-F PIC X(4).
       01 LK-P TYPE TT.
       PROCEDURE DIVISION USING LK-P.
       MAIN.
           CONTINUE.
       END METHOD SWAPT.
       END FACTORY.
       END CLASS PB1012NC.
