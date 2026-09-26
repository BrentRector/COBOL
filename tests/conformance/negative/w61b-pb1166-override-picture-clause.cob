      *> reject-at: 2002 2014 2023
      *> kb/Work PB1166 - the OVERRIDE lane (§11.7.3 SR9 through §9.3.8.2.3
      *> rules 3/6), the REJECT half.
      *>   cite.py --check 9.3.8.2.3 "has the same ALIGNED, ANY LENGTH,
      *>     BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and
      *>     USAGE clauses"                                         OK 3)
      *>   cite.py --check 9.3.8.2.3 "Period picture symbols match if and
      *>     only if the DECIMAL-POINT IS COMMA clause is in effect for
      *>     both or for neither of these interfaces"               OK 3) b)
      *>   cite.py --check 8.5.3.1 "Currency symbols match if and only if
      *>     the corresponding currency strings are the same"       OK 1)
      *> The base class says CURRENCY "E" under $ and DECIMAL-POINT IS
      *> COMMA; the subclass says neither. Each override repeats its base
      *> method's character-string exactly, so only the named axis differs:
      *>   MC  PIC $$9,99 - "E" vs "$" (and DPC for one only).
      *>   MA  PIC A(4) overridden by PIC X(4) - two PICTURE clauses.
      *>   MR  RETURNING ZZ9,99 - rule 6, DPC for one only.
      *> Each is COBOLNET0829 on the overriding method.
       IDENTIFICATION DIVISION.
       CLASS-ID. W61BNEGOVA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "E" WITH PICTURE SYMBOL "$"
           DECIMAL-POINT IS COMMA.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1 PIC $$9,99.
       PROCEDURE DIVISION USING L1.
           CONTINUE.
       END METHOD MC.
       METHOD-ID. MA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L2 PIC A(4).
       PROCEDURE DIVISION USING L2.
           CONTINUE.
       END METHOD MA.
       METHOD-ID. MR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L3 PIC ZZ9,99.
       PROCEDURE DIVISION RETURNING L3.
           MOVE 1 TO L3.
       END METHOD MR.
       END OBJECT.
       END CLASS W61BNEGOVA.
       IDENTIFICATION DIVISION.
       CLASS-ID. W61BNEGOVB INHERITS FROM W61BNEGOVA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS W61BNEGOVA.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MC OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1 PIC $$9,99.
       PROCEDURE DIVISION USING L1.
           CONTINUE.
       END METHOD MC.
       METHOD-ID. MA OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L2 PIC X(4).
       PROCEDURE DIVISION USING L2.
           CONTINUE.
       END METHOD MA.
       METHOD-ID. MR OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L3 PIC ZZ9,99.
       PROCEDURE DIVISION RETURNING L3.
           MOVE 1 TO L3.
       END METHOD MR.
       END OBJECT.
       END CLASS W61BNEGOVB.
