*>--- conformance-case
*> id: pb3120-group-formal-template-pointer-signature-mismatch
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3120, review finding R1-1 (sibling) - the converse of
      *> 2002/pb3120_group_formal_template_pointer_signature: comparing a
      *> restricted data-pointer's TYPEDEF TEMPLATE by its program-pointer
      *> member's prototype SIGNATURE (13.18.60.4 GR25; cite.py --check
      *> 13.18.60.4 -> OK 13.18.60.4 25)) must still find a difference.
      *> QA takes a PIC 9(4), QB a PIC X(4): two signatures, so PX's
      *> prototype and its definition do not have "the same" signature
      *> and 10.6.2 SR2 (OK 10.6.2 2)) refuses the pair (COBOLNET1513).
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
       01 L-Y PIC X(4).
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
