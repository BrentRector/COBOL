      *>--- conformance-case
      *> id: l1c05_oo_strong_type_same_type
      *> rules: [GR-9.3.8.2.3-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §9.3.8.2.3 rule 7 — strongly-typed formal and returning
      *>   items of the SAME type
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
      *> cite.py --check 10.6.2 "The data division may contain only a
      *>   linkage section" -> OK  §10.6.2 4) e)  (Syntax rules)
      *> cite.py --check 11.8.3 "Each method prototype in each
      *>   implemented interface shall be such that the object interface
      *>   of this class conforms to all implemented interfaces" -> OK
      *>   §11.8.3 2)  (Syntax rules)
      *> The admitting arm: interface-2 = L1C05UI, whose SWAPT has a
      *>   USING formal and a RETURNING item of the strong type TT (a
      *>   TYPEDEF STRONG in the prototype's linkage section, the only
      *>   data section §10.6.2 SR4 e) allows); interface-1 = the object
      *>   interface of L1C05UC (IMPLEMENTS L1C05UI), whose SWAPT
      *>   declares an EQUIVALENT TT (§8.5.3.1: same type-name, same
      *>   STRONG, no EXTERNAL, same elementary layout) for both: the
      *>   same type, so it conforms, the program compiles and the call
      *>   runs. Rejecting arms:
      *>   negative/l1c05-oo-strong-type-other-name, -vs-plain-group,
      *>   -returning-other.
      *> Derivation: SWAPT receives "ABCD" -> "GOT=ABCD"; it returns
      *>   "WXYZ" -> "RET=WXYZ".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C05U.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C05UC
           INTERFACE L1C05UI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TT TYPEDEF STRONG.
          05 TT-F PIC X(4).
       01 W-C USAGE OBJECT REFERENCE L1C05UI.
       01 W-P TYPE TT.
       01 W-R TYPE TT.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE L1C05UC "NEW" RETURNING W-C.
           MOVE "ABCD" TO TT-F OF W-P.
           INVOKE W-C "SWAPT" USING W-P RETURNING W-R.
           DISPLAY "RET=" TT-F OF W-R.
           STOP RUN.
       END PROGRAM L1C05U.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. L1C05UI.
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
       END INTERFACE L1C05UI.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C05UC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE L1C05UI.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS L1C05UI.
       PROCEDURE DIVISION.
       METHOD-ID. SWAPT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TT TYPEDEF STRONG.
          05 TT-F PIC X(4).
       01 LK-P TYPE TT.
       01 LK-R TYPE TT.
       PROCEDURE DIVISION USING LK-P RETURNING LK-R.
       MAIN.
           DISPLAY "GOT=" TT-F OF LK-P.
           MOVE "WXYZ" TO TT-F OF LK-R.
       END METHOD SWAPT.
       END OBJECT.
       END CLASS L1C05UC.
