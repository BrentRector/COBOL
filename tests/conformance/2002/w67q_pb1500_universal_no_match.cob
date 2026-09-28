      *> kb/Work PB1500 - ISO §9.3.6 METHOD RESOLUTION through a universal
      *> object reference. The match rules are conditions of resolution:
      *> rule 1 "The number of invocation parameters shall be equal to the
      *> number of parameters defined in the invoked method" (RETURNING
      *> present on both sides or neither), rule 3 c) "is the same class
      *> and category". A method that does not match is not bound; the
      *> search continues up the INHERITS chain (§9.3.6 2)), and when no
      *> class matches, "6) otherwise, the EC-OO-METHOD exception
      *> condition is set to exist" (§14.9.23.4 GR7 b)). The declarative
      *> on EC-OO-METHOD therefore selects each non-matching INVOKE and
      *> RESUME AT NEXT STATEMENT continues the run unit; the one matching
      *> INVOKE binds and runs. W67QNMD inherits T from W67QNMC, so its
      *> non-matching INVOKE reaches the root through the chain.
      >>TURN EC-OO-METHOD CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W67QNM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS W67QNMC
           CLASS W67QNMD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U  USAGE OBJECT REFERENCE.
       01 N3 PIC 9(3) VALUE 7.
       01 X3 PIC X(3) VALUE "ABC".
       01 R3 PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO-METHOD.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE W67QNMC "NEW" RETURNING U.
      *>   rule 1: one argument for two non-OPTIONAL formals
           INVOKE U "T" USING N3.
           DISPLAY "AFTER-ARITY".
      *>   rule 1: a RETURNING item where the method declares none
           INVOKE U "M" RETURNING R3.
           DISPLAY "AFTER-RETURNING".
      *>   rule 3: the two arguments' descriptions swapped
           INVOKE U "T" USING X3 N3.
           DISPLAY "AFTER-DESCRIPTION".
      *>   the matching invocation is bound and runs
           INVOKE U "T" USING N3 X3.
           DISPLAY "AFTER-MATCH".
      *>   §9.3.6 2): the subclass declares no T; the inherited T does
      *>   not match either, so the search ends at step 6)
           INVOKE W67QNMD "NEW" RETURNING U.
           INVOKE U "T" USING N3.
           DISPLAY "AFTER-INHERITED".
           INVOKE U "T" USING N3 X3.
           STOP RUN.
       END PROGRAM W67QNM.

       IDENTIFICATION DIVISION.
       CLASS-ID. W67QNMC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       PROCEDURE DIVISION.
           DISPLAY "M RAN".
       END METHOD M.
       METHOD-ID. T.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC 9(3).
       01 LX PIC X(3).
       PROCEDURE DIVISION USING LN LX.
           DISPLAY "T:" LN ":" LX.
       END METHOD T.
       END OBJECT.
       END CLASS W67QNMC.

       IDENTIFICATION DIVISION.
       CLASS-ID. W67QNMD INHERITS FROM W67QNMC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS W67QNMC.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. D.
       PROCEDURE DIVISION.
           DISPLAY "D RAN".
       END METHOD D.
       END OBJECT.
       END CLASS W67QNMD.
