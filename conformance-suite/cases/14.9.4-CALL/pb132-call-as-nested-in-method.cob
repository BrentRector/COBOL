*>--- conformance-case
*> id: pb132-call-as-nested-in-method
*> rules: [SR-14.9.4.3-13]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR13's OTHER context arm: a METHOD definition is not a program definition either
      *> (the function arm is pb132-call-as-nested-in-function; one predicate, both contexts).
       IDENTIFICATION DIVISION.
       CLASS-ID. CPB132M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB132M.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M1.
       PROCEDURE DIVISION.
       P.
           CALL "X" AS NESTED
           GOBACK.
       END METHOD M1.
       END OBJECT.
       END CLASS CPB132M.
