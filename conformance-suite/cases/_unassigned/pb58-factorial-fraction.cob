*>--- conformance-case
*> id: pb58-factorial-fraction
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> PB58 - the argument-screen predicate the class column could not carry. ISO 15.36.3 r1: "an integer greater than or equal to zero" - a scaled item is not an integer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGFACTORIALFRACTION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X3    PIC X(3) VALUE "ABC".
       01 X1    PIC X VALUE "A".
       01 N9    PIC 9(3) VALUE 5.
       01 N9V   PIC 9(3)V99 VALUE 5.5.
       01 S9    PIC S9(3) VALUE 5.
       01 C9    PIC 9(3) COMP VALUE 5.
       01 NAT   PIC N(2) VALUE N"AB".
       01 R     PIC S9(9)V99.
       01 T     PIC 9(4).
       01 A20   PIC X(20).
       01 TDEF  TYPEDEF STRONG.
          05 TF PIC X(2).
       01 SG    TYPE TDEF.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION FACTORIAL(N9V).
           STOP RUN.
