      *> kb/Work PB480 (train 1021 review D finding 1) - a method bound through a UNIVERSAL object reference
      *> whose OPTIONAL formal is described with ANY LENGTH. 14.9.23.4 GR7 c): "If identifier-1 is a
      *> universal object reference and the method being invoked is a COBOL method, neither a formal
      *> parameter nor the returning item in the invoked method shall be described with the ANY LENGTH
      *> clause" (cite.py --check 14.9.23.4 -> OK 14.9.23.4 7)) - a ban on the method's DESCRIPTION, so
      *> an OMITTED argument, which 9.3.6 match rule 3 b) admits against an OPTIONAL formal ("No further
      *> checking is performed on this parameter and this parameter is considered to match exactly",
      *> OK 9.3.6 3) b)), still binds a method GR7 c) forbids: EC-OO-UNIVERSAL, never a crash.
      *> 9.3.6 match rule 1: a trailing OPTIONAL formal with no argument is an equal number of
      *> parameters (OK 9.3.6 1)). Match rule 3 e): "the corresponding parameter has the same ALIGNED,
      *> ANY LENGTH, ..." clauses (OK 9.3.6 3)), so a PIC X(4) argument does not match an ANY LENGTH
      *> formal and "6) otherwise, the EC-OO-METHOD exception condition is set to exist" (OK 9.3.6 6)).
      *> Before the fix the two omitted legs threw System.Diagnostics.UnreachableException.
      *> DERIVATION:
      *>   M USING X        PIC X(4) vs ANY LENGTH: no match         -> HANDLED=EC-OO-METHOD
      *>   M USING OMITTED  rule 3 b) match; GR7 c) violated         -> HANDLED=EC-OO-UNIVERSAL
      *>   M (no argument)  rule 1 match; GR7 c) violated            -> HANDLED=EC-OO-UNIVERSAL
       >>TURN EC-OO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB480AO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C480AO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U  USAGE OBJECT REFERENCE.
       01 X  PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE C480AO "NEW" RETURNING U
           INVOKE U "M" USING X
           INVOKE U "M" USING OMITTED
           INVOKE U "M"
           DISPLAY "END".
           STOP RUN.
       END PROGRAM PB480AO.

       IDENTIFICATION DIVISION.
       CLASS-ID. C480AO INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS C480AO.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA PIC X ANY LENGTH.
       PROCEDURE DIVISION USING OPTIONAL LA.
           DISPLAY "M:REACHED".
       END METHOD M.
       END OBJECT.
       END CLASS C480AO.
