      *>--- conformance-case
      *> id: l1-sin-class-numeric
      *> rules: [AR-15.82.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 15.82.3 r1: "Argument-1 shall be of class numeric."
      *> NOTHING in the corpus screened SIN. The fixture next door,
      *> pb1-numeric-arg-trig-family, covers ACOS, ASIN and ATAN - the
      *> three INVERSE functions - and its own comment says so, but
      *> its NAME implies the whole trig family, which is exactly how
      *> COS, SIN and TAN sat unscreened while a reader scanning the
      *> corpus would have read them as covered (pb52's comment says
      *> the same thing about the same fixture). This file and its TAN
      *> sibling close the half the name only implied.
      *>
      *> TWO OPERANDS, ONE RULE, BOTH DECIDABLE AT BIND TIME.
      *> (1) PIC X(3) is class ALPHANUMERIC (ISO 8.5.2.1 Table 2).
      *> (2) PIC ZZ9.99 is category NUMERIC-EDITED, which Table 2 puts
      *> under class ALPHANUMERIC when its usage is display - the
      *> rule says CLASS, so however numeric the item looks it is not
      *> a legal argument-1. A screen written on CATEGORY rather than
      *> CLASS passes the first line and fails only on the second.
      *>
      *> Edition-invariant: SIN is a COBOL-85 Intrinsic Function
      *> Module member (unlike ACOS/ASIN/ATAN, which are post-85 and
      *> are therefore rejected at 85 for a DIFFERENT reason - which
      *> is why pb1's reject-at header omits 85 and this one does
      *> not), and 15.82.3 r1 has no edition-conditional wording.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SINCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(3) VALUE "abc".
       01 E PIC ZZ9.99.
       01 R PIC S9(6)V99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION SIN(A).
           COMPUTE R = FUNCTION SIN(E).
           STOP RUN.
