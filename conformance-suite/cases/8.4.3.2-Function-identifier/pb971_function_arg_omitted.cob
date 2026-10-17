*>--- conformance-case
*> id: pb971_function_arg_omitted
*> rules: [GR-8.4.3.2.4-8]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB971 - ISO 8.4.3.2.4 GR8: "If a parameter for which the
      *> omitted-argument condition is true is referenced in an activated
      *> function, except as an argument or in the omitted-argument condition,
      *> the EC-FUNCTION-ARG-OMITTED exception condition is set to exist and the
      *> results of the execution of the function are undefined." The name is
      *> the FUNCTION one - never EC-PROGRAM-ARG-OMITTED - because the rule is
      *> keyed to the kind of the activated element. Both references below (X,
      *> and G1 under the group formal G) raise it; the function's own
      *> declarative reports each and RESUME AT NEXT STATEMENT abandons the
      *> interrupted MOVE, so R keeps "XOMT" from the IS OMITTED test, which
      *> raises nothing. The second activation supplies both arguments.
      >>TURN EC-FUNCTION-ARG-OMITTED CHECKING ON
       IDENTIFICATION DIVISION.
       FUNCTION-ID. P971FN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(2) VALUE 0.
       LINKAGE SECTION.
       01 X PIC X(4).
       01 G.
          05 G1 PIC X(2).
          05 G2 PIC X(2).
       01 R PIC X(4).
       PROCEDURE DIVISION USING OPTIONAL X OPTIONAL G RETURNING R.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-FUNCTION-ARG-OMITTED.
       H-P.
           ADD 1 TO N
           DISPLAY "  F-CAUGHT " N "=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           MOVE "----" TO R
           IF X IS OMITTED MOVE "XOMT" TO R END-IF
           MOVE X TO R
           MOVE G1 TO R
           GOBACK.
       END FUNCTION P971FN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P971FM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION P971FN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       01 WA PIC X(4) VALUE "ABCD".
       01 WG PIC X(4) VALUE "GHIJ".
       PROCEDURE DIVISION.
           MOVE FUNCTION P971FN(OMITTED, OMITTED) TO W
           DISPLAY "W=[" W "]"
           MOVE FUNCTION P971FN(WA, WG) TO W
           DISPLAY "W=[" W "]"
           STOP RUN.
       END PROGRAM P971FM.
