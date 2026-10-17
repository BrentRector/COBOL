*>--- conformance-case
*> id: pb75_sdidi_overflow_outside_arithmetic
*> rules: [GR-8.8.1.5.2-2, GR-14.6.13.1.3-4, GR-14.6.13.1.3-5, GR-14.7.5-2, GR-14.7.5-7, GR-14.7.5-L3.3, DOC-A.1-70]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB75 - a size error raised OUTSIDE an arithmetic statement is the fatal exception condition 14.7.5's
      *> no-phrase rules name, not a raw CLR crash. 14.7.5: the size error condition "may occur as a result of ...
      *> the evaluation of an arithmetic expression", and with no SIZE ERROR phrase EC-SIZE-OVERFLOW "is set to
      *> exist, and processing proceeds as specified in 14.6.13.1.3": #5 - a USE declarative for the condition runs
      *> (RESUME AT NEXT STATEMENT continues after the offending statement - 14.9.33.4 GR2a3: "the lowest level
      *> statement, not the containing statement", NOTE 1: after the END-IF even though neither branch ran); #4 - an
      *> enclosing exception-checking PERFORM's WHEN handler runs (its RESUME AT NEXT STATEMENT likewise; without one
      *> the run unit terminates abnormally after the handler); and an arithmetic statement's own ON SIZE ERROR
      *> phrase still takes precedence (#1). ONE dispatch per raise: the PERFORM's own guard lets the condition its
      *> inner statement already dispatched pass (before this landing every enclosing statement re-dispatched it -
      *> the USE declarative ran twice for one raise inside a PERFORM). Under STANDARD-DECIMAL 10 ** 100000 exceeds decimal128 (8.8.1.5.2 r2) in a CONDITION, a FUNCTION
      *> ARGUMENT, and a MOVE sender - each used to die with an unhandled CobolSizeError, exit 127.
      *> (With checking OFF the same raise reaches the run-unit boundary and terminates loudly, exit 1 -
      *> 14.6.13.1.3 #8, the implementor's documented choice; pinned by SdidiOverflowDispositionTests.)
      >>TURN EC-SIZE-OVERFLOW CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB75SDOVF.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC 9(5).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE-OVERFLOW.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "R1".
           IF 10 ** 100000 > 5 DISPLAY "GT" ELSE DISPLAY "LE" END-IF.
           DISPLAY "R2".
           DISPLAY "V=" FUNCTION ABS(10 ** 100000).
           DISPLAY "R3".
           MOVE FUNCTION INTEGER-PART(10 ** 100000) TO WS-X.
           DISPLAY "R4 X=" WS-X.
           PERFORM
               IF 10 ** 100000 > 5 DISPLAY "GT2" END-IF
           WHEN EC-SIZE-OVERFLOW
               DISPLAY "WHEN=" FUNCTION EXCEPTION-STATUS
               RESUME AT NEXT STATEMENT
           END-PERFORM.
           DISPLAY "R5".
           COMPUTE WS-X = 10 ** 100000
               ON SIZE ERROR DISPLAY "PHRASE=" FUNCTION EXCEPTION-STATUS
               NOT ON SIZE ERROR DISPLAY "NOSIZE"
           END-COMPUTE.
           DISPLAY "DONE".
           STOP RUN.
