      *> ISO §14.6.4 3) 5) — inline method invocation and object property evaluation precede reference modification (7)
      *> RULE (§14.6.4, cite.py OK 3) and 5)): "The item identification
      *> steps that are applicable to that identifier are evaluated in the
      *> following order: ... 3) inline method invocation ... 5) object
      *> property evaluation ... 7) reference modification". Also "If a
      *> step in the evaluation of an identifier requires evaluation of
      *> another identifier or an arithmetic expression, that evaluation
      *> is done in full before proceeding to the next step." Agrees with
      *> §8.4.3.1.4 1) e) / d) before g) "a reference modifier applies to
      *> the identifier on the left" (cite.py OK).
      *> INSTRUMENT: the EXTERNAL counter L1IID-K. The factory method GETV
      *> and the get property method PV each CALL L1IIDBMP (K += 1) and
      *> return "wxyz"; the user function L1IIDCTR does K += 1 and returns
      *> the new K. The leftmost position of the reference modifier is
      *> FUNCTION L1IIDCTR, so it reads K AFTER whatever ran before it.
      *> DERIVATION, line 1 (rule 3): K=0. Step 3 runs GETV first: K=1.
      *>   Step 7 then evaluates the modifier: L1IIDCTR -> K=2, returns 2.
      *>   "wxyz"(2:1) = "x"; MOVE to X PIC X(4) pads -> "x   ".
      *>   -> METHOD X=x   |K=2
      *>   (Wrong order would give L1IIDCTR=1 first -> "w", K=2.)
      *> DERIVATION, line 2 (rule 5): identical with the property get
      *>   -> PROPERTY X=x   |K=2
      *> EDITION: OO, user-defined functions and properties are 2002+;
      *> placed at the introducing edition 2002 (identical at 2014/2023).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. L1IIDCTR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L1IID-K PIC 9 EXTERNAL.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R.
           ADD 1 TO L1IID-K.
           MOVE L1IID-K TO R.
           GOBACK.
       END FUNCTION L1IIDCTR.
       IDENTIFICATION DIVISION.
       CLASS-ID. L1IIDCLS.
       FACTORY.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. GETV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC X(4).
       PROCEDURE DIVISION RETURNING R.
           CALL "L1IIDBMP".
           MOVE "wxyz" TO R.
           GOBACK.
       END METHOD GETV.
       IDENTIFICATION DIVISION.
       METHOD-ID. GET PROPERTY PV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC X(4).
       PROCEDURE DIVISION RETURNING R.
           CALL "L1IIDBMP".
           MOVE "wxyz" TO R.
           GOBACK.
       END METHOD.
       END FACTORY.
       END CLASS L1IIDCLS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1IIDMN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1IIDCLS
           FUNCTION L1IIDCTR
           PROPERTY PV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L1IID-K PIC 9 EXTERNAL.
       01 X PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 0 TO L1IID-K.
           MOVE L1IIDCLS :: "GETV" (FUNCTION L1IIDCTR : 1) TO X.
           DISPLAY "METHOD X=" X "|K=" L1IID-K.
           MOVE 0 TO L1IID-K.
           MOVE PV OF L1IIDCLS (FUNCTION L1IIDCTR : 1) TO X.
           DISPLAY "PROPERTY X=" X "|K=" L1IID-K.
           STOP RUN.
       END PROGRAM L1IIDMN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1IIDBMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L1IID-K PIC 9 EXTERNAL.
       PROCEDURE DIVISION.
           ADD 1 TO L1IID-K.
           GOBACK.
       END PROGRAM L1IIDBMP.
