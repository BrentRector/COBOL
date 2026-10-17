      *>--- conformance-case
      *> id: pb3101_bit_group_signature_equivalence
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3101 (review finding R1-1) - "the same signature" is
      *> an EQUIVALENCE. 13.18.60.4 GR25: a restricted program-pointer
      *> "shall contain only the predefined address NULL or the address
      *> of a program with the same signature as that identified by the
      *> specified program-prototype-name". The formals of PA (a bit
      *> group of 4 + 4), PB (PIC 1(8) USAGE BIT) and PC (a bit group of
      *> 2 + 6) all describe ONE item: 13.18.29.4 GR1 b) "Unless stated
      *> otherwise, a bit group is treated as though it were an
      *> elementary data item of usage bit ... described with PICTURE
      *> 1(m)", and 13.7.3 SR2 ("shall match the description") states
      *> nothing otherwise. So all three SETs conform (14.9.39.3 SR22),
      *> not only the two through PTR-B: the relation once compared two
      *> bit groups by their subordinate split while comparing a bit
      *> group to PIC 1(8) by the as-if picture, so PA = PB, PB = PC and
      *> PA /= PC, and SET PTR-C TO PTR-A was refused (COBOLNET1959).
      *> The CALL through Q3101 passes PTR-A (restricted to PA) for a
      *> formal restricted to PC: 14.8.2.3.2 rule 4 "both shall be
      *> restricted and of the same type" asks the pointers' signature
      *> CLASS, which the founder-based partition split by presentation
      *> order (COBOLNET1688). QD3101 sees the NULL PTR-A holds.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PA IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-G GROUP-USAGE BIT.
          05 L-G1 PIC 1(4).
          05 L-G2 PIC 1(4).
       PROCEDURE DIVISION USING L-G.
       END PROGRAM PA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-E PIC 1(8) USAGE BIT.
       PROCEDURE DIVISION USING L-E.
       END PROGRAM PB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PC IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-H GROUP-USAGE BIT.
          05 L-H1 PIC 1(2).
          05 L-H2 PIC 1(6).
       PROCEDURE DIVISION USING L-H.
       END PROGRAM PC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. Q3101 AS "Q3101X" IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T-P IS TYPEDEF USAGE PROGRAM-POINTER TO PC.
       01 L-P TYPE T-P.
       PROCEDURE DIVISION USING L-P.
       END PROGRAM Q3101.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3101T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PA
           PROGRAM PB
           PROGRAM PC
           PROGRAM Q3101 AS "Q3101X".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-A IS TYPEDEF USAGE PROGRAM-POINTER TO PA.
       01 T-B IS TYPEDEF USAGE PROGRAM-POINTER TO PB.
       01 T-C IS TYPEDEF USAGE PROGRAM-POINTER TO PC.
       01 PTR-A TYPE T-A.
       01 PTR-B TYPE T-B.
       01 PTR-C TYPE T-C.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET PTR-A TO NULL
           SET PTR-B TO PTR-A
           SET PTR-C TO PTR-B
           SET PTR-C TO PTR-A
           DISPLAY "SET OK"
           CALL Q3101 USING PTR-A
           STOP RUN.
       END PROGRAM P3101T.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. QD3101 AS "Q3101X".
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-P IS TYPEDEF USAGE PROGRAM-POINTER TO PC.
       LINKAGE SECTION.
       01 L-P TYPE T-P.
       PROCEDURE DIVISION USING L-P.
       P-MAIN.
           IF L-P = NULL
               DISPLAY "Q GOT NULL"
           ELSE
               DISPLAY "Q GOT ADDRESS"
           END-IF
           GOBACK.
       END PROGRAM QD3101.
