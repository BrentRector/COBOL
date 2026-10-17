*>--- conformance-case
*> id: pb132-call-bit-variable-subscript
*> rules: [SR-14.9.4.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR6 (second clause): a BY REFERENCE bit item's subscripts shall consist of only
      *> fixed-point numeric literals or all-literal arithmetic expressions without exponentiation - a
      *> variable subscript leaves the referenced address unprovable at compile time.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB132N7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          02 BT PIC 1(8) USAGE BIT OCCURS 3.
       01 I PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN.
           CALL "SUB" USING BT(I)
           STOP RUN.
