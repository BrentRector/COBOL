*>--- conformance-case
*> id: ec_program_ptr_null
*> rules: [GR-14.9.4.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.4.4 GR3b names TWO DISTINCT conditions for a CALL that cannot proceed: "If the data item
      *> referenced by identifier-1 contains the predefined address NULL, the EC-PROGRAM-PTR-NULL exception
      *> condition is set to exist. If the program cannot be located or identifier-1 references a zero-length
      *> item, the EC-PROGRAM-NOT-FOUND exception condition is set to exist." A NULL program-pointer is the
      *> FIRST of them. (GR3g's "invalid program address … undefined" governs a NON-null bad address.)
      *> Table 13: EC-PROGRAM-PTR-NULL is Fatal, so RESUME AT NEXT STATEMENT (§14.9.33) is what keeps the run
      *> unit alive past the failed CALL — without it §14.6.13.1.3 #5 terminates abnormally after the
      *> declarative. That no-RESUME arm is covered by
      *> ExceptionConditionConformanceTests.CallPointerNull_Enabled_NoHandler_FatalTerminates.
      *>
      *> Two defects had to be fixed together for this to work, and either alone leaves it broken: the runtime
      *> raised EC-PROGRAM-NOT-FOUND for the NULL case (so this declarative could never select), and
      *> EC-PROGRAM-PTR-NULL was absent from the binder's EC-PROGRAM family list (so the CALL was emitted with
      *> no guard at all to catch it).
      >>TURN EC-PROGRAM-PTR-NULL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ECPTRNULL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PPTR USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM-PTR-NULL.
       H-P.
           DISPLAY "HANDLED-PTR-NULL".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           SET PPTR TO NULL.
           CALL PPTR.
           DISPLAY "AFTER".
           STOP RUN.
