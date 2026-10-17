      *>--- conformance-case
      *> id: l1-log10-class-numeric
      *> rules: [AR-15.56.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 15.56.3 r1: "Argument-1 shall be of class numeric." Its
      *> positive side is pinned by pb19 (FUNCTION LOG10(1000)) and
      *> pb56 (FUNCTION LOG10(100)); nothing exercised the rejection,
      *> which is the whole content of a "shall" clause.
      *>
      *> TWO OPERANDS, ONE RULE, BOTH DECIDABLE AT BIND TIME.
      *> (1) PIC X(3) is class ALPHANUMERIC (ISO 8.5.2.1 Table 2).
      *> (2) PIC ZZ9.99 is category NUMERIC-EDITED, which Table 2 puts
      *> under class ALPHANUMERIC when its usage is display - the
      *> rule says CLASS, so however numeric the item looks it is not
      *> a legal argument-1. A screen written on CATEGORY rather than
      *> CLASS passes the first line and fails only on the second.
      *>
      *> Note this rule is 15.56.3, LOG10's own - NOT 15.55.3, which
      *> is LOG's and carries word-for-word identical text. The two
      *> clauses are separate inventory rows and each is cited from
      *> its own function's clause, never inherited from its twin.
      *>
      *> Edition-invariant: LOG10 is a COBOL-85 Intrinsic Function
      *> Module member and 15.56.3 r1 has no edition-conditional
      *> wording, so the rejection is owed at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1L10CLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(3) VALUE "abc".
       01 E PIC ZZ9.99.
       01 R PIC S9(6)V99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION LOG10(A).
           COMPUTE R = FUNCTION LOG10(E).
           STOP RUN.
