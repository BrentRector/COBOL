*>--- conformance-case
*> id: pb3120_universal_type_subject_key
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3120, review finding R1-1 - the universal-dispatch key
      *> of a strongly-typed record that holds a pointer restricted to
      *> its own type is the SAME string whether it is built from the
      *> TYPE subject (the argument ADDRESS OF S) or from the TYPEDEF
      *> template (the method's formal LINKAGE 01 LP TYPE PT).
      *> 8.4.3.11.4 GR2 (cite.py --check 8.4.3.11.4 -> OK 8.4.3.11.4 2))
      *> "If identifier-1 is a strongly-typed group item or a restricted
      *> data-pointer, the data-address-identifier is a restricted
      *> data-pointer that is restricted to the type of identifier-1".
      *> DERIVATION:
      *>   ADDRESS OF S is restricted to the type of S, T-REC.
      *>   TR's formal LP is restricted to T-REC (PT is POINTER TO
      *>     T-REC), declared in class C3120RV with the same layout:
      *>     equivalent declarations (8.5.3.1, OK 8.5.3.1), so one type
      *>     and 14.8.2.2 rule 2 (OK 14.8.2.2 2)) admits the argument:
      *>     TR runs (TR:REACHED).
      *>   Class C3120RW declares its T-REC with T-X PIC X(3): not
      *>     equivalent (a different length at the same position), so
      *>     the invocation selects no method: EC-OO-METHOD is raised
      *>     and the declarative resumes at the next statement.
       >>TURN EC-OO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3120RV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C3120RV
           CLASS C3120RW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 T-REC IS TYPEDEF STRONG.
          05 T-X PIC X(2).
          05 T-N TYPE PT.
       01 S TYPE T-REC.
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
           INVOKE C3120RV "NEW" RETURNING U
           INVOKE U "TR" USING ADDRESS OF S
           INVOKE C3120RW "NEW" RETURNING U
           INVOKE U "TR" USING ADDRESS OF S
           DISPLAY "END".
           STOP RUN.
       END PROGRAM PB3120RV.

       IDENTIFICATION DIVISION.
       CLASS-ID. C3120RV INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS C3120RV.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 T-REC IS TYPEDEF STRONG.
          05 T-X PIC X(2).
          05 T-N TYPE PT.
       PROCEDURE DIVISION.
       METHOD-ID. TR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE PT.
       PROCEDURE DIVISION USING LP.
           DISPLAY "TR:REACHED".
       END METHOD TR.
       END OBJECT.
       END CLASS C3120RV.

       IDENTIFICATION DIVISION.
       CLASS-ID. C3120RW INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS C3120RW.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 T-REC IS TYPEDEF STRONG.
          05 T-X PIC X(3).
          05 T-N TYPE PT.
       PROCEDURE DIVISION.
       METHOD-ID. TR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE PT.
       PROCEDURE DIVISION USING LP.
           DISPLAY "TR2:WRONG".
       END METHOD TR.
       END OBJECT.
       END CLASS C3120RW.
