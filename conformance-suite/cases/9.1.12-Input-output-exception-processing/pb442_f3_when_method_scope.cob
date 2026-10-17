*>--- conformance-case
*> id: pb442_f3_when_method_scope
*> rules: [GR-9.1.12-1, GR-14.9.28.4-17]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB442 - an exception-checking PERFORM's WHEN phrase never selects a condition raised in
      *> a METHOD it INVOKEs, whatever the method contains (docs/CONFORMANCE.md §3 D-F3S).
      *>
      *> THE RULES, and the expected values derived from them (no observation):
      *>  14.6.13.1.1 - an exception status indicator "exists for each function, method, or program": the
      *>    READ past end of file in method RDR raises EC-I-O-AT-END (checking is enabled by the >>TURN
      *>    written before the class) in the METHOD's runtime element. 14.6.13.1.4 then applies there:
      *>    no conditional phrase, no exception-checking PERFORM of the method's own is executing, no USE
      *>    statement, so 4) execution continues - RDR-AFTER with the at-end status 10 (9.1.13).
      *>  14.9.18.4 GR1 b) - a condition reaches the activator only when it is propagated (RAISING); RDR
      *>    propagates nothing, so the driver's WHEN EC-I-O-AT-END never runs and imperative-statement-1
      *>    completes: DRV-AFTER, then DRV-DONE.
      *>
      *> WHY THE LEG CAN FAIL: before kb/Work PB442 a method of a class that holds an exception-checking
      *> PERFORM (OTHER3) consulted the run unit's whole frame stack at an I-O raise and ran the DRIVER's
      *> WHEN inside the method (DRV-WHEN-LEAK, then the method went on).
      *>
      *> EDITIONS: 2023 only - the exception-checking PERFORM is a 2023 facility; the edition gate is
      *> negative/pb441-f3-perform-below-2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB442MDRV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB442MCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB442MCLS.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB442MCLS "NEW" RETURNING O
           PERFORM
               INVOKE O "RDR"
               DISPLAY "DRV-AFTER"
           WHEN EC-I-O-AT-END
               DISPLAY "DRV-WHEN-LEAK"
           END-PERFORM
           DISPLAY "DRV-DONE"
           STOP RUN.
       END PROGRAM PB442MDRV.
       >>TURN EC-I-O CHECKING ON
       IDENTIFICATION DIVISION.
       CLASS-ID. PB442MCLS INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb442mio.dat"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS F-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 F-ST PIC XX.
       PROCEDURE DIVISION.
       METHOD-ID. RDR.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           CLOSE F
           OPEN INPUT F
           READ F
           DISPLAY "RDR-AFTER " F-ST
           CLOSE F.
       END METHOD RDR.
       METHOD-ID. OTHER3.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM
               DISPLAY "OTHER3"
           WHEN EC-SIZE-OVERFLOW
               DISPLAY "OTHER3-WHEN"
           END-PERFORM.
       END METHOD OTHER3.
       END OBJECT.
       END CLASS PB442MCLS.
