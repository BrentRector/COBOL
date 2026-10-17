      *>--- conformance-case
      *> id: pb1012_strong_type_override_factory
      *> rules: [GR-9.3.8.2.3-3, GR-9.3.8.2.3-4, GR-9.3.8.2.3-6,
      *>    GR-9.3.8.2.3-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §9.3.8.2.3 rule 7 over its two other askers — an OVERRIDE
      *>   (§11.7.3 SR9) and a FACTORY IMPLEMENTS (§11.4.3 SR2) — with
      *>   strongly-typed formal and returning items of the SAME type
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
      *> cite.py --check 11.7.3 "the parameter declarations, returning
      *>   item, and exceptions that may be raised on the procedure
      *>   division header shall obey the rules of conformance
      *>   according to 9.3.8.2.3" -> OK  §11.7.3 9)  (Syntax rules)
      *> cite.py --check 11.4.3 "Each method prototype in each
      *>   implemented interface shall be such that the factory
      *>   interface of this class conforms to all implemented
      *>   interfaces" -> OK  §11.4.3 2)  (Syntax rules)
      *> cite.py --check 10.6.2 "The data division may contain only a
      *>   linkage section" -> OK  §10.6.2 4) e)  (Syntax rules)
      *> The IMPLEMENTS-on-OBJECT asker is
      *>   conformance:cases/9.3.8.2.3-Conformance-between-interfaces/l1c05_oo_strong_type_same_type. Here:
      *>   1) the FACTORY of PB1012A IMPLEMENTS PB1012I, whose FSWAP
      *>      prototype takes and returns the strong type TT (declared
      *>      in the prototype's linkage section, the only data section
      *>      §10.6.2 SR4 e) allows); the factory method declares an
      *>      EQUIVALENT TT (§8.5.3.1: same type-name, STRONG on both,
      *>      no EXTERNAL, one PIC X(4) at the same position), so the
      *>      factory interface conforms;
      *>   2) PB1012B's SHOW overrides PB1012A's with an equivalent TT
      *>      formal, an identical PIC 9(3)V99 formal and an identical
      *>      PIC X(6) RETURNING item (rules 3, 4 and 6 as well), so
      *>      the override conforms and dispatches to PB1012B.
      *> Derivation: FSWAP receives "ABCD" -> "FGOT=ABCD" and returns
      *>   "WXYZ" -> "FRET=WXYZ"; SHOW through a PB1012A reference to a
      *>   PB1012B object runs the override: TT-F "PQRS" and 123.45 in
      *>   PIC 9(3)V99 (DISPLAY shows its five digits, no point) ->
      *>   "BGOT=PQRS 12345", returning "OVERRD" -> "SRET=OVERRD".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1012P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1012A
           CLASS PB1012B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TT TYPEDEF STRONG.
          05 TT-F PIC X(4).
       01 W-O USAGE OBJECT REFERENCE PB1012A.
       01 W-P TYPE TT.
       01 W-R TYPE TT.
       01 W-N PIC 9(3)V99.
       01 W-S PIC X(6).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABCD" TO TT-F OF W-P.
           INVOKE PB1012A "FSWAP" USING W-P RETURNING W-R.
           DISPLAY "FRET=" TT-F OF W-R.
           INVOKE PB1012B "NEW" RETURNING W-O.
           MOVE "PQRS" TO TT-F OF W-P.
           MOVE 123.45 TO W-N.
           INVOKE W-O "SHOW" USING W-P W-N RETURNING W-S.
           DISPLAY "SRET=" W-S.
           STOP RUN.
       END PROGRAM PB1012P.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB1012I.
       PROCEDURE DIVISION.
       METHOD-ID. FSWAP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TT TYPEDEF STRONG.
          05 TT-F PIC X(4).
       01 LK-P TYPE TT.
       01 LK-R TYPE TT.
       PROCEDURE DIVISION USING LK-P RETURNING LK-R.
       END METHOD FSWAP.
       END INTERFACE PB1012I.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1012A INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE PB1012I.
       IDENTIFICATION DIVISION.
       FACTORY. IMPLEMENTS PB1012I.
       PROCEDURE DIVISION.
       METHOD-ID. FSWAP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TT TYPEDEF STRONG.
          05 TT-F PIC X(4).
       01 LK-P TYPE TT.
       01 LK-R TYPE TT.
       PROCEDURE DIVISION USING LK-P RETURNING LK-R.
       MAIN.
           DISPLAY "FGOT=" TT-F OF LK-P.
           MOVE "WXYZ" TO TT-F OF LK-R.
       END METHOD FSWAP.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TT TYPEDEF STRONG.
          05 TT-F PIC X(4).
       01 LK-T TYPE TT.
       01 LK-N PIC 9(3)V99.
       01 LK-R PIC X(6).
       PROCEDURE DIVISION USING LK-T LK-N RETURNING LK-R.
       MAIN.
           DISPLAY "AGOT=" TT-F OF LK-T " " LK-N.
           MOVE "BASEA" TO LK-R.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB1012A.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1012B INHERITS FROM PB1012A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1012A.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TT TYPEDEF STRONG.
          05 TT-F PIC X(4).
       01 LK-T TYPE TT.
       01 LK-N PIC 9(3)V99.
       01 LK-R PIC X(6).
       PROCEDURE DIVISION USING LK-T LK-N RETURNING LK-R.
       MAIN.
           DISPLAY "BGOT=" TT-F OF LK-T " " LK-N.
           MOVE "OVERRD" TO LK-R.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB1012B.
