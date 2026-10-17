*>--- conformance-case
*> id: pb952-argument-domain-checking-at-85
*> rules: [AR-15.8.3-2, AR-15.10.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 15.3 rule 14's EC-ARGUMENT-FUNCTION is observable only with checking ENABLED ("If the
      *> EC-ARGUMENT-FUNCTION exception condition is set to exist and checking for EC-ARGUMENT-FUNCTION is not
      *> enabled, the implementor defines the result"), and >>TURN is the 2002 exception-checking directive, so
      *> below 2002 the bound-adjacent domain witness cannot be written: the directive is the edition gate's
      *> COBOLNET0900. The positive twin is 2002/pb952_argument_domain_bound_adjacent (kb/Work PB952), which
      *> screens ACOS(1 + 10**-30) - the closest fixed-point argument past +1 - to EC-ARGUMENT-FUNCTION.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB952AT85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ABOVE-ONE PIC S9V9(30) VALUE 1.000000000000000000000000000001.
       01 W-R       PIC S9V9(6).
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE W-R = FUNCTION ACOS(ABOVE-ONE).
           STOP RUN.
