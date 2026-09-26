      *> ISO §14.9.23.4 GR2 b) — a non-COBOL method is never invoked:
      *> the universal-receiver arm of the DOC-A.1-101 determination.
      *> RULE (cite.py --check 14.9.23.4 OK, GR 2 b)): "If the method
      *> to be invoked is a non-COBOL method, the behavior of the INVOKE
      *> statement is implementor-defined." Annex A.1 item 101 requires
      *> it documented; docs/CONFORMANCE.md DOC-A.1-101 determines: "on
      *> a universal object reference ... the runtime dispatch searches
      *> only the COBOL-declared methods of the object's class
      *> hierarchy, finds none, and sets EC-OO-METHOD".
      *> THE ARM AT RISK: every .NET object carries System.Object's
      *> ToString. A universal receiver defers method location to run
      *> time (SR4 applies only to a non-universal identifier-1:
      *> cite.py --check 14.9.23.3 OK, SR 4), so no syntax rule stops
      *> INVOKE U "ToString" and only the documented determination
      *> decides whether the .NET member is reached.
      *> DERIVATION of every .out line:
      *>   L1IV101V declares only HELLO; BASE's instance interface is
      *>   FactoryObject only (§16.2, cite.py OK). Method resolution
      *>   (§9.3.6, cite.py OK, step 6): "otherwise, the EC-OO-METHOD
      *>   exception condition is set to exist." §14.9.23.4 GR7 b)
      *>   (cite.py OK): "If the method is not found ... the
      *>   EC-OO-METHOD exception condition is set to exist, the method
      *>   invocation is not successful, and execution continues as
      *>   specified in General rule 7g."
      *>   EC-OO-METHOD is fatal (Table 13); the >>TURN enables it, so
      *>   §14.6.13.1.3 5) (cite.py OK) runs the USE declarative:
      *>   EC-OO-METHOD      printed by the declarative.
      *>   RESUME AT NEXT STATEMENT (§14.9.33.4 GR2 a) 1., cite.py OK):
      *>   the applicable statement is the INVOKE itself, so control
      *>   continues after it:
      *>   AFTER             printed by the next statement.
      *> An implementation that reached System.Object.ToString would
      *> print AFTER alone.
      *> 2002 dir: INVOKE, >>TURN, USE EXCEPTION CONDITION and RESUME
      *> are COBOL-2002.
       >>TURN EC-OO-METHOD CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1IV101U.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1IV101V.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HM SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO-METHOD.
       HM-P.
           DISPLAY "EC-OO-METHOD".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           INVOKE L1IV101V "NEW" RETURNING U
           INVOKE U "ToString"
           DISPLAY "AFTER"
           STOP RUN.
       END PROGRAM L1IV101U.
       IDENTIFICATION DIVISION.
       CLASS-ID. L1IV101V INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. HELLO.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "HELLO".
       END METHOD HELLO.
       END OBJECT.
       END CLASS L1IV101V.
