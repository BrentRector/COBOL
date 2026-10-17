*>--- conformance-case
*> id: oo_method_perform
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §11.7 METHOD-ID / §14.9.28 PERFORM — an OO instance METHOD whose body uses PERFORM (plain and TIMES) over its
      *> own paragraphs, mutating per-instance OBJECT data. Exercises the instance dispatch + per-instance State:
      *> the method's paragraphs and the Dispatch helper are instance members, called through the receiver.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOPERF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS TICKER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE OBJECT REFERENCE TICKER.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE TICKER "NEW" RETURNING T.
           INVOKE T "TICK".
           STOP RUN.
       END PROGRAM OOPERF.

       IDENTIFICATION DIVISION.
       CLASS-ID. TICKER INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. TICK.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM SHOW.
           PERFORM SHOW 2 TIMES.
           GOBACK.
       SHOW.
           ADD 1 TO N.
           DISPLAY "N=" N.
       END METHOD TICK.
       END OBJECT.
       END CLASS TICKER.
