*>--- conformance-case
*> id: udf-bare-arity
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R35's other verdict: a BARE reference to a REPOSITORY-declared TWO-argument function
      *> is an ARITY error about a function - the user DECLARED it a function, so there is no
      *> coincidental-collision reading and "undefined" would send them hunting a typo. 8.4.3.2.3 SR2
      *> makes the bare form a function reference with zero arguments; the prototype takes two.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. R35TWO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A1 PIC 9.
       01 A2 PIC 9.
       01 R-OUT PIC 9(4).
       PROCEDURE DIVISION USING A1 A2 RETURNING R-OUT.
           COMPUTE R-OUT = A1 + A2.
           GOBACK.
       END FUNCTION R35TWO.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. R35NEG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION R35TWO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9(4).
       PROCEDURE DIVISION.
           MOVE R35TWO TO X.
           STOP RUN.
