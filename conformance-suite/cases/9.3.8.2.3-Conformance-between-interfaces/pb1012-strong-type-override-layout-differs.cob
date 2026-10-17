      *>--- conformance-case
      *> id: pb1012-strong-type-override-layout-differs
      *> rules: [GR-9.3.8.2.3-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §9.3.8.2.3 rule 7 through §11.7.3 SR9 — an OVERRIDE whose
      *>   strong type has the SAME type-name but a different layout
      *> Rule: "If either of the corresponding formal parameters or
      *>   returning items in interface-1 or interface-2 is a
      *>   strongly-typed group item, both are of the same type."
      *> cite.py --check 9.3.8.2.3 "If either of the corresponding
      *>   formal parameters or returning items in interface-1 or
      *>   interface-2 is a strongly-typed group item, both are of the
      *>   same type" -> OK  §9.3.8.2.3 7)  (Conformance between
      *>   interfaces)
      *> cite.py --check 8.5.3.1 "starting at the same relative byte or
      *>   bit position and having the same length in bytes or bits"
      *>   -> OK  §8.5.3.1   (General)
      *> cite.py --check 11.7.3 "the parameter declarations, returning
      *>   item, and exceptions that may be raised on the procedure
      *>   division header shall obey the rules of conformance
      *>   according to 9.3.8.2.3" -> OK  §11.7.3 9)  (Syntax rules)
      *> PB1012NB's SWAPT overrides PB1012NA's (interface-1 is the
      *>   override's object interface, interface-2 the overridden
      *>   one's). Both formals are TYPE TT, a TYPEDEF STRONG of the
      *>   same type-name, but TT-F is PIC X(4) in PB1012NA and PIC
      *>   X(5) in PB1012NB: the elementary items do not have the same
      *>   length, so the declarations are not equivalent (§8.5.3.1),
      *>   the formals are not of the same type and §11.7.3 SR9 is
      *>   violated: COBOLNET0829.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1012NA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SWAPT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TT TYPEDEF STRONG.
          05 TT-F PIC X(4).
       01 LK-P TYPE TT.
       PROCEDURE DIVISION USING LK-P.
       MAIN.
           CONTINUE.
       END METHOD SWAPT.
       END OBJECT.
       END CLASS PB1012NA.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1012NB INHERITS FROM PB1012NA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1012NA.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SWAPT OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TT TYPEDEF STRONG.
          05 TT-F PIC X(5).
       01 LK-P TYPE TT.
       PROCEDURE DIVISION USING LK-P.
       MAIN.
           CONTINUE.
       END METHOD SWAPT.
       END OBJECT.
       END CLASS PB1012NB.
