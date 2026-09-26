      *> reject-at: 2002 2014 2023
      *> kb/Work PB1166 - the IMPLEMENTS lane (§9.3.11 through §9.3.8.2.3
      *> rule 3), the REJECT half.
      *>   cite.py --check 9.3.8.2.3 "Period picture symbols match if and
      *>     only if the DECIMAL-POINT IS COMMA clause is in effect for
      *>     both or for neither of these interfaces"               OK 3) b)
      *>   cite.py --check 8.5.3.1 "Currency symbols match if and only if
      *>     the corresponding currency strings are the same"       OK 1)
      *> The interface says CURRENCY "EUR" under F; the class says "GBP"
      *> under F. The prototype and the implementation write the same
      *> character-string F9.99 (equal length, so no width accident can
      *> refuse it), and M2 repeats PIC A(4) as PIC X(4). Each makes the
      *> class not conform to the interface it IMPLEMENTS: COBOLNET0841.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. W61BNEGIMI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "EUR" WITH PICTURE SYMBOL "F".
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC F9.99.
       PROCEDURE DIVISION USING A.
       END METHOD M1.
       METHOD-ID. M2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 B PIC A(4).
       PROCEDURE DIVISION USING B.
       END METHOD M2.
       END INTERFACE W61BNEGIMI.
       IDENTIFICATION DIVISION.
       CLASS-ID. W61BNEGIMC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "GBP" WITH PICTURE SYMBOL "F".
       REPOSITORY.
           CLASS BASE
           INTERFACE W61BNEGIMI.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS W61BNEGIMI.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC F9.99.
       PROCEDURE DIVISION USING A.
           CONTINUE.
       END METHOD M1.
       METHOD-ID. M2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 B PIC X(4).
       PROCEDURE DIVISION USING B.
           CONTINUE.
       END METHOD M2.
       END OBJECT.
       END CLASS W61BNEGIMC.
