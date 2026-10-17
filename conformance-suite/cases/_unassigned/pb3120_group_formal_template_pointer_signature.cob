      *>--- conformance-case
      *> id: pb3120_group_formal_template_pointer_signature
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3120, review finding R1-1 (sibling) - a data-pointer
      *> restricted to a strongly-typed record is compared through that
      *> type's TYPEDEF TEMPLATE, whose program-pointer member is typed
      *> by its prototype's SIGNATURE, never by the name it was written
      *> with (13.18.60.4 GR25; cite.py --check 13.18.60.4 -> OK
      *> 13.18.60.4 25)). 8.5.3.1 (OK 8.5.3.1): equivalent type
      *> declarations pair corresponding elementary items of the same
      *> USAGE.
      *> DERIVATION:
      *>   PX's prototype: T-REC holds a program-pointer TO QA, TG holds
      *>     a pointer restricted TO T-REC. PX's definition: T-REC holds
      *>     one TO QB. QA and QB both take one PIC 9(4): one signature.
      *>   10.6.2 SR2 (OK 10.6.2 2)) "the signatures of these two
      *>     compilation units shall be the same": they are, so the unit
      *>     compiles, and CALL PX USING G (14.8.2.2 rule 2, OK
      *>     14.8.2.2 2)) runs PX, which displays PX RAN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. QA IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM QA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. QB IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Y PIC 9(4).
       PROCEDURE DIVISION USING L-Y.
       END PROGRAM QB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PX IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM QA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO QA.
       01 T-REC IS TYPEDEF STRONG.
          05 R-P TYPE T1.
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE PT.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       END PROGRAM PX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3120D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM QA
           PROGRAM PX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TA IS TYPEDEF USAGE PROGRAM-POINTER TO QA.
       01 T-REC IS TYPEDEF STRONG.
          05 R-P TYPE TA.
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 TG IS TYPEDEF STRONG.
          05 M-P TYPE PT.
       01 G TYPE TG.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL PX USING G
           STOP RUN.
       END PROGRAM PB3120D.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM QB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T2 IS TYPEDEF USAGE PROGRAM-POINTER TO QB.
       01 T-REC IS TYPEDEF STRONG.
          05 R-P TYPE T2.
       01 PT IS TYPEDEF USAGE POINTER TO T-REC.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE PT.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       MAIN-P.
           DISPLAY "PX RAN"
           GOBACK.
       END PROGRAM PX.
