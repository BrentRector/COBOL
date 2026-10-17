      *>--- conformance-case
      *> id: pb1549_set_program_address_fatal_resume
      *> rules: [GR-8.4.3.13.4-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1549 - a failed SET ... TO ADDRESS OF PROGRAM raises
      *> the FATAL EC-PROGRAM-NOT-FOUND, and only a RESUME lets the run
      *> unit continue past it. (The unhandled arm - abnormal run-unit
      *> termination - is asserted with its exit code by
      *> FatalRaiseSelectionTests; a corpus golden must exit 0.)
      *>
      *> THE RULES.
      *> §8.4.3.13.4 GR4: "If the runtime system cannot locate the
      *>   program, the EC-PROGRAM-NOT-FOUND exception condition is set
      *>   to exist and the value of the address-identifier is the
      *>   predefined address NULL."
      *>   OK  §8.4.3.13.4 4)  (General rules)
      *> §14.6.13.1.3 5): with checking enabled and an applicable USE
      *>   statement "the associated declarative is executed. If
      *>   execution of the declarative completes normally the
      *>   execution of the run unit is terminated abnormally" - NOTE 2:
      *>   "The user is able to continue by using a RESUME statement".
      *>   OK  §14.6.13.1.3 5)  (Fatal exception conditions)
      *> §14.9.33.4 GR2: NEXT STATEMENT transfers control to an
      *>   implicit CONTINUE that "immediately follows the end of the
      *>   statement that was executing" - here, the failed SET.
      *>   OK  §14.9.33.4 2) a)  (General rules)
      *> §14.9.33.4 GR3: "If procedure-name-1 is specified, control is
      *>   transferred to procedure-name-1 as if a GO TO
      *>   procedure-name-1 were executed."
      *>   OK  §14.9.33.4 3)  (General rules)
      *>
      *> DERIVATION.
      *> PQ=SET -- PB1549B is a program of this run unit, so ADDRESS OF
      *>   PROGRAM locates it; no condition is raised.
      *> NF-HANDLED -- PB1549Z is not in the run unit: GR4 raises
      *>   EC-PROGRAM-NOT-FOUND, the declarative runs (5)), and
      *>   WS-MODE = "N" takes RESUME AT NEXT STATEMENT (GR2) ...
      *> PP=NULL -- ... so the IF after the SET runs, and GR4 made the
      *>   address-identifier NULL.
      *> EC1=EC-PROGRAM-NOT-FOUND -- the last exception status (checking
      *>   is enabled, §14.6.13.1.1).
      *> NF-HANDLED -- the second failed SET runs the declarative again;
      *>   WS-MODE = "P" now takes RESUME AT M-AFTER (GR3), so the
      *>   DISPLAY "NOT-REACHED" after the SET never runs ...
      *> AT-M-AFTER -- ... and control continues at M-AFTER.
       >>TURN EC-PROGRAM-NOT-FOUND CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1549A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PP USAGE PROGRAM-POINTER.
       01 PQ USAGE PROGRAM-POINTER.
       01 WS-MODE PIC X VALUE "N".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-NF SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM-NOT-FOUND.
       D-NF-P.
           DISPLAY "NF-HANDLED"
           IF WS-MODE = "N"
               RESUME AT NEXT STATEMENT
           ELSE
               RESUME AT M-AFTER
           END-IF.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           SET PQ TO ADDRESS OF PROGRAM "PB1549B"
           IF PQ = NULL
               DISPLAY "PQ=NULL"
           ELSE
               DISPLAY "PQ=SET"
           END-IF
           SET PP TO ADDRESS OF PROGRAM "PB1549Z"
           IF PP = NULL
               DISPLAY "PP=NULL"
           ELSE
               DISPLAY "PP=SET"
           END-IF
           DISPLAY "EC1=" FUNCTION EXCEPTION-STATUS
           MOVE "P" TO WS-MODE
           SET PP TO ADDRESS OF PROGRAM "PB1549Z"
           DISPLAY "NOT-REACHED".
       M-AFTER.
           DISPLAY "AT-M-AFTER"
           STOP RUN.
       END PROGRAM PB1549A.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1549B.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "IN-PB1549B"
           GOBACK.
       END PROGRAM PB1549B.
