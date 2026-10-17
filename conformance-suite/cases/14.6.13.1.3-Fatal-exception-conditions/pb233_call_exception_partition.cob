*>--- conformance-case
*> id: pb233_call_exception_partition
*> rules: [GR-14.6.13.1.3-1, GR-14.9.4.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB233 - ISO 14.9.4.4 GR3h routes a FAILED ACTIVATION three ways, and the emitted CALL used
      *> to conflate the three facts the routing turns on. This golden pins the clean-running arms; the
      *> abnormal-termination arms (which a corpus golden cannot express) are
      *> ExceptionConditionConformanceTests.CallNotOnExceptionOnly_ActivationFails_TerminatesLikeAPhraselessCall,
      *> .CallOnException_IsIgnoredWhenTheFailureIsTheCalleesOwn and .CallOnException_IsNotEnteredByFunctionNotFound.
      *>
      *> A - GR3h item 2, FIRST disjunct: "If checking for the exception condition is enabled, and if the
      *>     exception condition is one of the EC-PROGRAM or EC-EXTERNAL exception conditions and an ON
      *>     EXCEPTION phrase is NOT specified ... any applicable exception processing statements are
      *>     executed. If control is returned from these statements, control is then transferred to the end of
      *>     the CALL statement." NOT ON EXCEPTION is not an ON EXCEPTION phrase - 14.6.13.1.3 #1 admits only
      *>     "a conditional phrase WITHOUT the NOT phrase" - so the declarative runs and, control having
      *>     returned to the END of the CALL, imperative-statement-2 does NOT. Before PB233 the mere presence
      *>     of the NOT phrase captured the failure: no declarative, no diagnostic, no termination.
      *> B - GR3h item 1 with the phrase present: "control is transferred to imperative-statement-1", and
      *>     14.6.13.1.3 #1 makes the conditional phrase win over the declarative even under enabled checking.
      *>     So B-DECL is NOT displayed - the same declarative that fired for A stands down here.
      *> C - GR3i on a SUCCESSFUL call: "control is transferred to the end of the CALL statement or, if the
      *>     NOT ON EXCEPTION phrase is specified, to imperative-statement-2".
      *> D - 8.4.3.2.4 GR6b -> GR6f: a user-function locate miss sets EC-FUNCTION-NOT-FOUND and "any
      *>     declarative ... associated with that exception condition is executed". This condition is in
      *>     NEITHER family GR3h item 1 names, and before PB233 it had no handling arm of any kind: the CALL
      *>     emitter filtered its enabled-name set down to EC-PROGRAM-*/EC-EXTERNAL-*, and the binder never
      *>     even queried the name, so >>TURN EC-FUNCTION-NOT-FOUND CHECKING ON wrapped nothing.
       >>TURN EC-PROGRAM-NOT-FOUND CHECKING ON
       >>TURN EC-FUNCTION-NOT-FOUND CHECKING ON
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB233ABSENTFN IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-A PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-A RETURNING L-R.
       END FUNCTION PB233ABSENTFN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB233PARTITION.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB233ABSENTFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC 9(4) VALUE 5.
       01 W-R PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H-PROG SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM-NOT-FOUND.
       H-PROG-P.
           DISPLAY "DECL-PROG".
           RESUME AT NEXT STATEMENT.
       H-FUNC SECTION.
           USE AFTER EXCEPTION CONDITION EC-FUNCTION-NOT-FOUND.
       H-FUNC-P.
           DISPLAY "DECL-FUNC".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
      *> A - NOT ON EXCEPTION only, activation fails: the declarative runs, A-NOT does not.
           CALL "PB233NOSUCH"
               NOT ON EXCEPTION DISPLAY "A-NOT"
           END-CALL.
           DISPLAY "A-AFTER".
      *> B - ON EXCEPTION present: imperative-statement-1 runs and the declarative stands down.
           CALL "PB233NOSUCH"
               ON EXCEPTION DISPLAY "B-ON"
               NOT ON EXCEPTION DISPLAY "B-NOT"
           END-CALL.
           DISPLAY "B-AFTER".
      *> C - a successful call: the ON phrase is ignored (GR3i) and imperative-statement-2 runs.
           CALL "PB233OK"
               ON EXCEPTION DISPLAY "C-ON"
               NOT ON EXCEPTION DISPLAY "C-NOT"
           END-CALL.
           DISPLAY "C-AFTER".
      *> D - a user-function locate miss reaches ITS declarative (8.4.3.2.4 GR6f).
           MOVE FUNCTION PB233ABSENTFN(W-X) TO W-R.
           DISPLAY "D-AFTER".
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB233OK.
       PROCEDURE DIVISION.
       OK-P.
           DISPLAY "C-CALLEE".
           GOBACK.
       END PROGRAM PB233OK.
       END PROGRAM PB233PARTITION.
