      *>--- conformance-case
      *> id: pb2495-recursive-prototype-signature-mismatch
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2495 - the converse of 2002/pb2495_recursive_
      *> prototype_signature: comparing prototypes whose formals are
      *> restricted program-pointers as the greatest fixed point must
      *> still find a difference reached by a finite path. NS1 (USING
      *> pointer TO NS2) and NS2 (USING pointer TO NS1) are one
      *> signature; NX1 (USING pointer TO NX2), NX2 (USING pointer TO
      *> NX3), NX3 (USING 9(4)) is another - two steps down, NX3's 9(4)
      *> meets NS1's pointer (13.18.60.4 GR25; cite.py --check
      *> 13.18.60.4 -> OK 13.18.60.4 25)). So 14.9.39.3 SR22 (OK
      *> 14.9.39.3 22)) - "the program-prototypes associated with
      *> identifier-7 and identifier-8 shall have the same signature" -
      *> refuses SET X TO A (COBOLNET1959).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NS1 IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM NS2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T2 IS TYPEDEF USAGE PROGRAM-POINTER TO NS2.
       01 L-P TYPE T2.
       PROCEDURE DIVISION USING L-P.
       END PROGRAM NS1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NS2 IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM NS1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T1 IS TYPEDEF USAGE PROGRAM-POINTER TO NS1.
       01 L-Q TYPE T1.
       PROCEDURE DIVISION USING L-Q.
       END PROGRAM NS2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NX1 IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM NX2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TX2 IS TYPEDEF USAGE PROGRAM-POINTER TO NX2.
       01 L-D TYPE TX2.
       PROCEDURE DIVISION USING L-D.
       END PROGRAM NX1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NX2 IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM NX3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TX3 IS TYPEDEF USAGE PROGRAM-POINTER TO NX3.
       01 L-E TYPE TX3.
       PROCEDURE DIVISION USING L-E.
       END PROGRAM NX2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NX3 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-F PIC 9(4).
       PROCEDURE DIVISION USING L-F.
       END PROGRAM NX3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2495N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM NS1
           PROGRAM NX1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PT1 IS TYPEDEF USAGE PROGRAM-POINTER TO NS1.
       01 PTX IS TYPEDEF USAGE PROGRAM-POINTER TO NX1.
       01 A TYPE PT1.
       01 X TYPE PTX.
       PROCEDURE DIVISION.
       MAIN-P.
           SET A TO NULL
           SET X TO A
           STOP RUN.
       END PROGRAM PB2495N.
