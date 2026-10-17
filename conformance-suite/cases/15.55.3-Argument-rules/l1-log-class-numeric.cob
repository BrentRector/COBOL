      *>--- conformance-case
      *> id: l1-log-class-numeric
      *> rules: [AR-15.55.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 15.55.3 r1: "Argument-1 shall be of class numeric." The
      *> POSITIVE side of this rule is already pinned (pb19 computes
      *> FUNCTION LOG(1) and pb56 FUNCTION LOG(1) under
      *> standard-decimal); this is the branch nothing exercised - the
      *> rejection a "shall" clause actually demands.
      *>
      *> TWO OPERANDS, ONE RULE, BOTH DECIDABLE AT BIND TIME.
      *> (1) PIC X(3) is class ALPHANUMERIC (ISO 8.5.2.1 Table 2), so
      *> it is not class numeric and the reference is illegal source.
      *> (2) PIC ZZ9.99 is category NUMERIC-EDITED, which Table 2 puts
      *> under class ALPHANUMERIC when its usage is display - the
      *> rule says CLASS, so however numeric the item looks it is not
      *> a legal argument-1. That second line is the one a screen
      *> written on CATEGORY instead of CLASS lets through.
      *>
      *> Edition-invariant: LOG is a COBOL-85 Intrinsic Function
      *> Module member (present at 85, 2002, 2014 and 2023) and
      *> 15.55.3 r1 carries no edition-conditional wording, so the
      *> rejection is owed at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1LOGCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(3) VALUE "abc".
       01 E PIC ZZ9.99.
       01 R PIC S9(6)V99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION LOG(A).
           COMPUTE R = FUNCTION LOG(E).
           STOP RUN.
