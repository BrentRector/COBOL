*>--- conformance-case
*> id: pb3129_type_pointer_placement_ok
*> rules: [SR-13.18.60.3-14]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3129 - where a pointer-class TYPE entry may stand, and the
      *> group that holds one crossing a CALL through a prototype pair.
      *> 13.18.60.3 SR14: a USAGE clause with the POINTER or PROGRAM-POINTER
      *> phrase "may be specified only for an elementary data item at level 1
      *> or an elementary data item subordinate to a type declaration that
      *> includes the STRONG phrase", and 13.18.57.4 GR1 makes a TYPE entry
      *> "as though the data description identified by type-name-1 had been
      *> coded in place of the TYPE clause". So L1 (level 1), L7 (level 77)
      *> and S-P (under the STRONG ST) are legal; the same TYPE at level 05 of
      *> an ordinary group is not (negative pb3129-type-pointer-in-weak-group).
      *> 14.8.2.2: a strongly-typed group formal and argument "shall be of the
      *> same type" - G and L-G are both TYPE ST, the prototype and the
      *> definition describe L-G alike (10.6.2 SR2), and BY REFERENCE the
      *> callee works on the caller's storage (14.2.3 GR8): it sees the null
      *> S-P and "INN", and the caller then sees the "OUT" it moved.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3129Q IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM PB3129Q.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3129PX IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PB3129Q.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO PB3129Q.
       01 ST IS TYPEDEF STRONG.
          05 S-P TYPE T1.
          05 S-X PIC X(3).
       01 L-G TYPE ST.
       PROCEDURE DIVISION USING L-G.
       END PROGRAM PB3129PX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3129M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PB3129Q
           PROGRAM PB3129PX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO PB3129Q.
       01 DT IS TYPEDEF USAGE POINTER.
       01 ST IS TYPEDEF STRONG.
          05 S-P TYPE T1.
          05 S-X PIC X(3).
       01 G TYPE ST.
       01 L1 TYPE DT.
       77 L7 TYPE DT.
       PROCEDURE DIVISION.
       MAIN-P.
           SET L1 TO NULL
           SET L7 TO L1
           IF L7 = NULL DISPLAY "L7 NULL" END-IF
           SET S-P IN G TO NULL
           MOVE "INN" TO S-X IN G
           CALL PB3129PX USING G
           DISPLAY "AFTER " S-X IN G
           STOP RUN.
       END PROGRAM PB3129M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3129PX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PB3129Q.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO PB3129Q.
       01 ST IS TYPEDEF STRONG.
          05 S-P TYPE T1.
          05 S-X PIC X(3).
       01 L-G TYPE ST.
       PROCEDURE DIVISION USING L-G.
       MAIN-P.
           IF S-P IN L-G = NULL DISPLAY "PX NULL " S-X IN L-G END-IF
           MOVE "OUT" TO S-X IN L-G
           GOBACK.
       END PROGRAM PB3129PX.
