*>--- conformance-case
*> id: pb403-exit-program-in-function
*> rules: [SR-14.9.14.3-7]
*> source-format: free
*> portable: unverified
*>---
*> ISO §14.9.14.3 SR7: "An EXIT PROGRAM statement may be specified only in a program procedure division."
*> §14.2.2 SR10 names the five source elements that may carry a Format-1/2 procedure division -- "a function
*> definition, a function prototype definition, a method definition, a program definition, or a program
*> prototype definition" -- so a FUNCTION definition's procedure division is not a program's, and this program
*> ends UXSR07's with EXIT PROGRAM.
*>
*> WHAT IT COST (kb/Work PB403). The binder's SR7 arm tested the single predicate `host.InMethod`, so exactly
*> ONE of the four non-program source elements was refused. This one was accepted -- and not merely tolerated:
*> the generated function class received the PROGRAM-activation machinery, a private `__asCalled` field and a
*> `throw new ProgramReturn()`, so program-shaped emission leaked into a function definition. That is the
*> two-arm shape with one arm fixed; the predicate the missing arm needed already existed in the same binder.
*>
*> 2002 is the floor because user-defined functions are a COBOL-2002 introduction: below it the FUNCTION-ID
*> paragraph itself is refused, so the non-program arm of SR7 is not reachable.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UXSR07.
       DATA DIVISION.
       LINKAGE SECTION.
       01  R-VAL   PIC 9(4).
       PROCEDURE DIVISION RETURNING R-VAL.
       F-MAIN.
           MOVE 14 TO R-VAL.
           EXIT PROGRAM.
       END FUNCTION UXSR07.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB403NEG3.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION UXSR07.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-X    PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION UXSR07 TO WS-X.
           DISPLAY "X=" WS-X.
           STOP RUN.
       END PROGRAM PB403NEG3.
