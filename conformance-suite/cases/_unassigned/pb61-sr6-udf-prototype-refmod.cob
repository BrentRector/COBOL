*>--- conformance-case
*> id: pb61-sr6-udf-prototype-refmod
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.2.3 SR6 names function-PROTOTYPE-name-1 as well as intrinsic-function-name-1: a user-defined
      *> function whose definition permits arguments (a USING formal) followed by '(' opens its ARGUMENT LIST, so
      *> `UPFN (1:4)` (keyword-omitted, the function declared in REPOSITORY) is an argument-list error (SR8) -
      *> not an arity error about zero arguments. The legal way to reference-modify the result writes the
      *> argument list first: UPFN("abcdefgh") (1:4).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UPFN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC X(8).
       01 R PIC X(8).
       PROCEDURE DIVISION USING A RETURNING R.
           MOVE FUNCTION UPPER-CASE(A) TO R
           GOBACK.
       END FUNCTION UPFN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB61SR6UDF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION UPFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T4 PIC X(4).
       PROCEDURE DIVISION.
           MOVE UPFN (1:4) TO T4
           STOP RUN.
       END PROGRAM PB61SR6UDF.
