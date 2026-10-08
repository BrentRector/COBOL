      *> reject-at: 2002 2014 2023
      *> kb/Work PB2464 - the converse of 2002/pb2464_prototype_pointer_
      *> signature_type: a restricted program-pointer is typed by its
      *> prototype's SIGNATURE (13.18.60.4 GR25; cite.py --check
      *> 13.18.60.4 -> OK 13.18.60.4 25)), so a typed INVOKE whose
      *> RETURNING item is restricted to a prototype of ANOTHER signature
      *> (P3N: USING X(4), against P1N: USING 9(4)) is not "of the same
      *> type" - 14.8.2.3.2 (OK 14.8.2.3.2 4)): "If either is a restricted
      *> pointer, both shall be restricted and of the same type" - and
      *> 14.8.3.3 (OK 14.8.3.3 2)) asks the same USAGE clause of the pair
      *> (COBOLNET0828). A differently NAMED prototype of the same
      *> signature is accepted: the positive golden.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1N IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM P1N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3N IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Z PIC X(4).
       PROCEDURE DIVISION USING L-Z.
       END PROGRAM P3N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2464T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P1N
           PROGRAM P3N
           CLASS C2464T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PT1 IS TYPEDEF USAGE PROGRAM-POINTER TO P1N.
       01 PT3 IS TYPEDEF USAGE PROGRAM-POINTER TO P3N.
       01 OC USAGE OBJECT REFERENCE C2464T.
       01 R3 TYPE PT3.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE C2464T "NEW" RETURNING OC
           INVOKE OC "RP1" RETURNING R3
           STOP RUN.
       END PROGRAM PB2464T.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2464T INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P1N
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PT1 IS TYPEDEF USAGE PROGRAM-POINTER TO P1N.
       PROCEDURE DIVISION.
       METHOD-ID. RP1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE PT1.
       PROCEDURE DIVISION RETURNING LP.
           SET LP TO NULL.
       END METHOD RP1.
       END OBJECT.
       END CLASS C2464T.
