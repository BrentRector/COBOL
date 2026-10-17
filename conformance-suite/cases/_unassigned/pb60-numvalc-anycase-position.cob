*>--- conformance-case
*> id: pb60-numvalc-anycase-position
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.68.2: ( argument-1 [argument-2] [ANYCASE] ) - ANYCASE is the
      *> LAST element and appears once. The old order-free keyword sweep accepted
      *> a doubled trailing ANYCASE (and an ANYCASE written before argument-1,
      *> which now binds as an operand per 8.10's context-sensitive-word rule and
      *> fails name resolution + arity). PB60 / FMT-15.68.2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB60NEGAC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(12) VALUE "1,234.56".
       01 R    PIC S9(9)V99.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION NUMVAL-C(WS-A "$" ANYCASE ANYCASE)
           STOP RUN.
