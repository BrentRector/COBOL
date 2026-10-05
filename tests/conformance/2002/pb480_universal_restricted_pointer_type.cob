      *> kb/Work PB480 (train 1021 review D finding 2) - a RESTRICTED data-pointer crossing a UNIVERSAL
      *> dispatch is restricted to a TYPE, and two type declarations of one type-name in two source
      *> elements are one type only when EQUIVALENT. 14.8.2.3.2: "If either is a restricted pointer,
      *> both shall be restricted and of the same type" (cite.py --check 14.8.2.3.2 -> OK 14.8.2.3.2 4));
      *> 8.5.3.1: "Two type declarations are considered equivalent when they have the same type-name, ...
      *> and for each elementary item in one type declaration there is a corresponding elementary item in
      *> the other type declaration, starting at the same relative byte or bit position and having the
      *> same length" (OK 8.5.3.1). 9.3.6 match rule 3 e) compares the USAGE clauses, a pointer's
      *> restriction among them (the reading conformance:2002/pb1408_universal_address_of_subordinate_group
      *> already pins), and "6) otherwise, the EC-OO-METHOD exception condition is set to exist" (OK 9.3.6 6)).
      *> The caller's T-REC (X(2) + X(4)) and the class's T-REC (one 9(6)) are not equivalent; C480RQ's
      *> T-REC is. Before the fix the restriction was keyed by its type-NAME only, so TR was reached.
      *> DERIVATION:
      *>   U  "TR" USING ADDRESS OF S   (restricted to the caller's T-REC): no match -> HANDLED=EC-OO-METHOD
      *>   U  "TR" USING RP             (TYPE PT, POINTER TO the caller's T-REC): no match
      *>                                                                       -> HANDLED=EC-OO-METHOD
      *>   V  "TR" USING ADDRESS OF S   (C480RQ: an equivalent T-REC): match     -> TR:REACHED
      *>   V  "TR" USING RP             match                                    -> TR:REACHED
       >>TURN EC-OO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB480RP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C480RP
           CLASS C480RQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 T-X PIC X(2).
          05 T-A PIC X(4).
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 S TYPE T-REC.
       01 RP TYPE PT.
       01 U USAGE OBJECT REFERENCE.
       01 V USAGE OBJECT REFERENCE.
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
           SET RP TO ADDRESS OF S
           INVOKE C480RP "NEW" RETURNING U
           INVOKE C480RQ "NEW" RETURNING V
           INVOKE U "TR" USING ADDRESS OF S
           INVOKE U "TR" USING RP
           INVOKE V "TR" USING ADDRESS OF S
           INVOKE V "TR" USING RP
           DISPLAY "END".
           STOP RUN.
       END PROGRAM PB480RP.

       IDENTIFICATION DIVISION.
       CLASS-ID. C480RP INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 T-N PIC 9(6).
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       PROCEDURE DIVISION.
       METHOD-ID. TR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE PT.
       PROCEDURE DIVISION USING LP.
           DISPLAY "TR:REACHED".
       END METHOD TR.
       END OBJECT.
       END CLASS C480RP.

       IDENTIFICATION DIVISION.
       CLASS-ID. C480RQ INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 T-X PIC X(2).
          05 T-A PIC X(4).
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       PROCEDURE DIVISION.
       METHOD-ID. TR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE PT.
       PROCEDURE DIVISION USING LP.
           DISPLAY "TR:REACHED".
       END METHOD TR.
       END OBJECT.
       END CLASS C480RQ.
