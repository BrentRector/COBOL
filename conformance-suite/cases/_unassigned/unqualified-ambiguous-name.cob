*>--- conformance-case
*> id: unqualified-ambiguous-name
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R33 - 8.4.2.2.1: "Qualification of a user-defined name is required unless ...
      *> 1) No other name has the identical spelling." Two declarations of DUPX under different
      *> groups, referenced unqualified: the reference identifies no unique resource. Previously
      *> resolved silently to the FIRST declaration (a wrong-answer shape - another compiler may
      *> pick differently); measured before enforcing: zero of 762 corpus+NIST programs hit this.
      *> --permissive keeps the traditional first-match, warned.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R33NEGA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 DUPX PIC 9 VALUE 1.
       01 G2.
          05 DUPX PIC 9 VALUE 2.
       01 R PIC 9.
       PROCEDURE DIVISION.
           MOVE DUPX TO R
           DISPLAY R.
           STOP RUN.
