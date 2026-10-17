      *>--- conformance-case
      *> id: pb2495_pointer_type_in_signature_checks
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2495 (sibling) - the TYPE of a restricted program-
      *> pointer is its prototype's signature (13.18.60.4 GR25; cite.py
      *> --check 13.18.60.4 -> OK 13.18.60.4 25)), and the group-level
      *> signature checks ask it too. Q1 and Q2 are two prototypes of
      *> one signature (USING 9(4)), so a pointer TO Q1 and a pointer
      *> TO Q2 are "of the same type" (14.8.2.3.2, OK 14.8.2.3.2 4)).
      *> DERIVATION:
      *>   PX's prototype (USING TO Q1) and definition (USING TO Q2) -
      *>     10.6.2 SR2 (OK 10.6.2 2)) "the signatures of these two
      *>     compilation units shall be the same": they are.
      *>   OVSUB's M1 OVERRIDE (USING TO Q2) over OVBASE's M1 (USING TO
      *>     Q1) - 11.7.3 SR9 (OK 11.7.3 9)) "shall obey the rules of
      *>     conformance according to 9.3.8.2.3": identical types.
      *>   OVSUB's M2 (USING TO Q2) implements IV2495's M2 (USING TO Q1)
      *>     - the same 9.3.8.2.3 rules: identical types.
      *> Each of the three ran before the pointer types were resolved
      *> and compared the prototype NAMES.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. Q1 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM Q1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. Q2 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Y PIC 9(4).
       PROCEDURE DIVISION USING L-Y.
       END PROGRAM Q2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PX IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO Q1.
       01 L-P TYPE T1.
       PROCEDURE DIVISION USING L-P.
       END PROGRAM PX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OVMAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q1
           PROGRAM PX
           CLASS OVSUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO Q1.
       01 A TYPE T1.
       01 O USAGE OBJECT REFERENCE OVSUB.
       PROCEDURE DIVISION.
       MAIN-P.
           SET A TO NULL
           CALL PX USING A
           INVOKE OVSUB "NEW" RETURNING O
           INVOKE O "M1" USING A
           INVOKE O "M2" USING A
           STOP RUN.
       END PROGRAM OVMAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T2 IS TYPEDEF USAGE PROGRAM-POINTER TO Q2.
       01 L-P TYPE T2.
       PROCEDURE DIVISION USING L-P.
       PX-P.
           IF L-P = NULL DISPLAY "px null" END-IF
           GOBACK.
       END PROGRAM PX.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. IV2495.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q1.
       PROCEDURE DIVISION.
       METHOD-ID. M2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO Q1.
       01 LP TYPE T1.
       PROCEDURE DIVISION USING LP.
       END METHOD M2.
       END INTERFACE IV2495.

       IDENTIFICATION DIVISION.
       CLASS-ID. OVBASE INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q1
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO Q1.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE T1.
       PROCEDURE DIVISION USING LP.
           DISPLAY "base WRONG".
       END METHOD M1.
       END OBJECT.
       END CLASS OVBASE.

       IDENTIFICATION DIVISION.
       CLASS-ID. OVSUB INHERITS FROM OVBASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q2
           CLASS OVBASE
           INTERFACE IV2495.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS IV2495.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T2 IS TYPEDEF USAGE PROGRAM-POINTER TO Q2.
       PROCEDURE DIVISION.
       METHOD-ID. M1 OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE T2.
       PROCEDURE DIVISION USING LP.
           IF LP = NULL DISPLAY "override null" END-IF.
       END METHOD M1.
       METHOD-ID. M2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE T2.
       PROCEDURE DIVISION USING LP.
           IF LP = NULL DISPLAY "implements null" END-IF.
       END METHOD M2.
       END OBJECT.
       END CLASS OVSUB.
