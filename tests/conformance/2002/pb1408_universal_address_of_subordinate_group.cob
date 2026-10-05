      *> kb/Work PB1408 + PB480 (train 1021) - "the type of identifier-1" in an
      *> ADDRESS OF argument crossing a UNIVERSAL dispatch. 8.4.3.11.4 GR2:
      *> "If identifier-1 is a strongly-typed group item or a restricted
      *> data-pointer, the data-address-identifier is a restricted
      *> data-pointer that is restricted to the type of identifier-1."
      *> (cite.py --check 8.4.3.11.4 -> OK 8.4.3.11.4 2)). T-G is a group
      *> subordinate to the strong type declaration T-REC, so by 8.5.3.1's
      *> second alternative its type is T-REC's declaration PLUS T-G's
      *> relative position and length in it - not T-REC itself.
      *> 9.3.6 match rule 3 e) compares the USAGE clauses (OK 9.3.6 3)), a
      *> pointer's restriction among them (14.8.2.3.2: "if either is a
      *> restricted pointer, both shall be restricted and of the same type"),
      *> and "6) otherwise, the EC-OO-METHOD exception condition is set to
      *> exist" (OK 9.3.6 6)).
      *> DERIVATION:
      *>   TR  ADDRESS OF S (type T-REC) into a formal TYPE PT
      *>       (POINTER TO T-REC): match              -> TR:REACHED
      *>   TQ  ADDRESS OF RP (RP TYPE PT: restricted to PT) into a formal
      *>       TYPE PPT (POINTER TO PT): match              -> TQ:REACHED
      *>   TR  ADDRESS OF T-G OF S: restricted to T-G's own type, which is
      *>       not T-REC: no method matches         -> HANDLED=EC-OO-METHOD
       >>TURN EC-OO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1408UA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C1408UA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 T-X PIC X(2).
          05 T-G.
             10 T-A PIC X(4).
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 S TYPE T-REC.
       01 RP TYPE PT.
       01 U USAGE OBJECT REFERENCE.
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
           MOVE "AB" TO T-X OF S
           MOVE "WXYZ" TO T-A OF T-G OF S
           SET RP TO ADDRESS OF S
           INVOKE C1408UA "NEW" RETURNING U
           INVOKE U "TR" USING ADDRESS OF S
           INVOKE U "TQ" USING ADDRESS OF RP
           INVOKE U "TR" USING ADDRESS OF T-G OF S
           DISPLAY "END".
           STOP RUN.
       END PROGRAM PB1408UA.

       IDENTIFICATION DIVISION.
       CLASS-ID. C1408UA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS C1408UA.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 T-X PIC X(2).
          05 T-G.
             10 T-A PIC X(4).
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 PPT IS TYPEDEF USAGE POINTER TO PT.
       PROCEDURE DIVISION.
       METHOD-ID. TR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE PT.
       PROCEDURE DIVISION USING LP.
           DISPLAY "TR:REACHED".
       END METHOD TR.
       METHOD-ID. TQ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LQ TYPE PPT.
       PROCEDURE DIVISION USING LQ.
           DISPLAY "TQ:REACHED".
       END METHOD TQ.
       END OBJECT.
       END CLASS C1408UA.
