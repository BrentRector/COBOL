      *> kb/Work PB1453 - an ADDRESS OF PROGRAM operand raises the fatal
      *> EC-PROGRAM-NOT-FOUND wherever the operand is written: in a
      *> relation condition (IF, EVALUATE WHEN, PERFORM UNTIL) and as an
      *> INVOKE argument, not only on SET and CALL. (The unhandled arms -
      *> abnormal run-unit termination - are asserted with their exit
      *> codes by FatalRaiseSelectionTests; a corpus golden must exit 0.)
      *>
      *> THE RULES.
      *> §8.4.3.13.4 GR4: "If the runtime system cannot locate the
      *>   program, the EC-PROGRAM-NOT-FOUND exception condition is set
      *>   to exist and the value of the address-identifier is the
      *>   predefined address NULL."
      *>   OK  §8.4.3.13.4 4)  (General rules)
      *> §14.6.13.1.1: "when the general rules for a statement indicate
      *>   that a specific exception condition exists, it is raised only
      *>   if checking for that exception condition is enabled." The
      *>   raise belongs to the DETECTION inside the statement, so the
      *>   statement kind does not matter.
      *>   OK  §14.6.13.1.1  (General)
      *> §14.6.13.1.3 5): with checking enabled and an applicable USE
      *>   statement "the associated declarative is executed"; NOTE 1 (after 4))
      *>   "The user is able to continue by using a RESUME statement".
      *>   OK  §14.6.13.1.3 5)  (Fatal exception conditions)
      *> §14.9.33.4 GR2 a) 1. and 3.: NEXT STATEMENT transfers to an
      *>   implicit CONTINUE that "immediately follows the end of the
      *>   statement that was executing"; "the applicable statement is
      *>   the one in which the exception condition was raised", and for
      *>   a contained statement "the lowest level statement, not the
      *>   containing statement"; NOTE 1: a condition raised while
      *>   evaluating the condition of an IF resumes "after the END-IF".
      *>   OK  §14.9.33.4 2) a)  (General rules)
      *>
      *> DERIVATION.
      *> CTRL-IF NOT-NULL -- PB1453A is a program of this run unit, so
      *>   ADDRESS OF PROGRAM locates it: no condition, the IF takes its
      *>   ELSE (GR4 gives a located program an address, not NULL).
      *> NF-HANDLED (1) then NO "REL-1" line -- PB1453Z is not in the
      *>   run unit: GR4 raises the condition while the IF's condition
      *>   is evaluated, the declarative runs (5)), RESUME AT NEXT
      *>   STATEMENT continues after END-IF, so neither arm of the IF
      *>   executes.
      *> EC1=EC-PROGRAM-NOT-FOUND -- the last exception status.
      *> NF-HANDLED (2), no "REL-2" -- the same through the
      *>   identifier form (GR1a: the program named by the content of
      *>   WS-NAME, "PB1453Y"), written as NOT = NULL.
      *> NF-HANDLED (3), no "EV" line -- an EVALUATE WHEN condition:
      *>   the applicable statement is the EVALUATE, resumed after
      *>   END-EVALUATE.
      *> NF-HANDLED (4), no "LOOP" line -- a PERFORM UNTIL condition,
      *>   resumed after END-PERFORM.
      *> INV-FOUND PROG -- an INVOKE argument that locates its program
      *>   passes a non-NULL program-pointer (the method answers PROG).
      *> NF-HANDLED (5) and INV-MISS NONE -- the failed argument raises
      *>   during the INVOKE: the declarative runs and RESUME AT NEXT
      *>   STATEMENT abandons the INVOKE, so the method never runs and
      *>   WS-R keeps the NONE it was given.
      *> EC2=EC-PROGRAM-NOT-FOUND -- the condition was set again.
       >>TURN EC-PROGRAM-NOT-FOUND CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1453A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1453K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-NAME PIC X(7) VALUE "PB1453Y".
       01 WS-R PIC X(4) VALUE "NONE".
       01 WS-O USAGE OBJECT REFERENCE PB1453K.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-NF SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM-NOT-FOUND.
       D-NF-P.
           DISPLAY "NF-HANDLED"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           IF ADDRESS OF PROGRAM "PB1453A" = NULL
               DISPLAY "CTRL-IF NULL"
           ELSE
               DISPLAY "CTRL-IF NOT-NULL"
           END-IF
           IF ADDRESS OF PROGRAM "PB1453Z" = NULL
               DISPLAY "REL-1 EQ"
           ELSE
               DISPLAY "REL-1 NE"
           END-IF
           DISPLAY "EC1=" FUNCTION EXCEPTION-STATUS
           IF ADDRESS OF PROGRAM WS-NAME NOT = NULL
               DISPLAY "REL-2 NE"
           ELSE
               DISPLAY "REL-2 EQ"
           END-IF
           EVALUATE TRUE
               WHEN ADDRESS OF PROGRAM "PB1453Z" = NULL
                   DISPLAY "EV EQ"
               WHEN OTHER
                   DISPLAY "EV OTHER"
           END-EVALUATE
           PERFORM UNTIL ADDRESS OF PROGRAM "PB1453Z" = NULL
               DISPLAY "LOOP"
           END-PERFORM
           INVOKE PB1453K "NEW" RETURNING WS-O
           INVOKE WS-O "WHICH" USING ADDRESS OF PROGRAM "PB1453A"
               RETURNING WS-R
           DISPLAY "INV-FOUND " WS-R
           MOVE "NONE" TO WS-R
           INVOKE WS-O "WHICH" USING ADDRESS OF PROGRAM "PB1453Z"
               RETURNING WS-R
           DISPLAY "INV-MISS " WS-R
           DISPLAY "EC2=" FUNCTION EXCEPTION-STATUS
           STOP RUN.
       END PROGRAM PB1453A.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1453K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WHICH.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LPP USAGE PROGRAM-POINTER.
       01 LR PIC X(4).
       PROCEDURE DIVISION USING LPP RETURNING LR.
           IF LPP = NULL MOVE "NULL" TO LR ELSE MOVE "PROG" TO LR.
           GOBACK.
       END METHOD WHICH.
       END OBJECT.
       END CLASS PB1453K.
