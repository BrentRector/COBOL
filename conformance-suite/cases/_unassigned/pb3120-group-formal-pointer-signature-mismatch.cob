      *>--- conformance-case
      *> id: pb3120-group-formal-pointer-signature-mismatch
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3120 - the converse of 2002/pb3120_group_formal_
      *> pointer_type: comparing a strongly-typed group formal's pointer
      *> by the SIGNATURE of its prototype (13.18.60.4 GR25; cite.py
      *> --check 13.18.60.4 -> OK 13.18.60.4 25)) must still find a
      *> difference. QA takes a PIC 9(4), QC a PIC X(4): two signatures,
      *> so PX's prototype (group member restricted TO QA) and its
      *> definition (TO QC) do not have "the same" signature, and
      *> 10.6.2 SR2 (OK 10.6.2 2)) refuses the pair (COBOLNET1513).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. QA IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM QA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. QC IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Y PIC X(4).
       PROCEDURE DIVISION USING L-Y.
       END PROGRAM QC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PX IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM QA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO QA.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE T1.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       END PROGRAM PX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3120N.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM PB3120N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM QC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T2 IS TYPEDEF USAGE PROGRAM-POINTER TO QC.
       01 TG IS TYPEDEF STRONG.
          05 L-P TYPE T2.
       01 L-G TYPE TG.
       PROCEDURE DIVISION USING L-G.
       MAIN-P.
           GOBACK.
       END PROGRAM PX.
