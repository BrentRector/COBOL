      *>--- conformance-case
      *> id: l1-tan-class-numeric
      *> rules: [AR-15.89.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 15.89.3 r1: "Argument-1 shall be of class numeric."
      *> NOTHING in the corpus screened TAN. pb1-numeric-arg-trig-
      *> family covers ACOS, ASIN and ATAN only (its own comment says
      *> so; its NAME does not) and pb52-intrinsic-argument-stage
      *> covers SQRT only, so a reader scanning for "is the trig
      *> family screened?" would have read TAN as covered while no
      *> spec-derived test named it at all.
      *>
      *> TWO OPERANDS, ONE RULE, BOTH DECIDABLE AT BIND TIME.
      *> (1) PIC X(3) is class ALPHANUMERIC (ISO 8.5.2.1 Table 2).
      *> (2) PIC ZZ9.99 is category NUMERIC-EDITED, which Table 2 puts
      *> under class ALPHANUMERIC when its usage is display - the
      *> rule says CLASS, so however numeric the item looks it is not
      *> a legal argument-1. A screen written on CATEGORY rather than
      *> CLASS passes the first line and fails only on the second.
      *>
      *> Edition-invariant: TAN is a COBOL-85 Intrinsic Function
      *> Module member and 15.89.3 r1 has no edition-conditional
      *> wording, so the rejection is owed at every edition. (This is
      *> why the reject-at header carries 85, where pb1's does not:
      *> ACOS/ASIN/ATAN are post-85 and fall at 85 to the edition
      *> gate instead of to the class screen.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TANCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(3) VALUE "abc".
       01 E PIC ZZ9.99.
       01 R PIC S9(6)V99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION TAN(A).
           COMPUTE R = FUNCTION TAN(E).
           STOP RUN.
