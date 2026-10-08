      *> kb/Work PB2659 - ISO 14.9.4.4 GR3 c): "If the program is
      *> located but the resources necessary to execute the program
      *> are not available, the EC-PROGRAM-RESOURCES exception
      *> condition is set to exist, the program call is not
      *> successful, and execution continues as specified in General
      *> rule 3h. The runtime resources that are checked in order to
      *> determine the availability of the called program for
      *> execution are defined by the implementor."
      *> Annex A.1 item 14 (docs/CONFORMANCE.md DOC-A.1-14): the one
      *> resource checked is the stack the activation needs, and a
      *> minimal RECURSIVE program reaches about 100,000 activations
      *> before the check fails.
      *> ISO 8.6.6: "A recursive program may call itself directly or
      *> indirectly." This one CALLs itself with no end:
      *> - every activation the stack can hold runs, so the depth
      *>   passes 50,000 (before PB2659 a CLR stack overflow killed
      *>   the process at a depth of about 250, with no exception
      *>   condition at all);
      *> - the one CALL the stack cannot hold is not successful:
      *>   GR3 h) item 1 runs its ON EXCEPTION phrase, and the last
      *>   exception status is EC-PROGRAM-RESOURCES because checking
      *>   for it is enabled (14.6.13.1.1);
      *> - every other CALL was successful, so GR3 i) ignores its ON
      *>   EXCEPTION phrase and each activation returns normally: the
      *>   phrase runs once and UNWOUND prints once, at the top.
      *> Fails if the process dies (no output), the depth is shallow,
      *> the status names another condition, or the phrase runs
      *> twice.
       >>TURN EC-PROGRAM-RESOURCES CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2659RES RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LEVEL     PIC 9(9) VALUE 0.
       01 MAX-LEVEL PIC 9(9) VALUE 0.
       PROCEDURE DIVISION.
           ADD 1 TO LEVEL
           IF LEVEL > MAX-LEVEL
               MOVE LEVEL TO MAX-LEVEL
           END-IF
           CALL "PB2659RES"
               ON EXCEPTION
                   DISPLAY "ON EXCEPTION " FUNCTION EXCEPTION-STATUS
           END-CALL
           SUBTRACT 1 FROM LEVEL
           IF LEVEL = 0
               IF MAX-LEVEL > 50000
                   DISPLAY "DEPTH OVER 50000"
               ELSE
                   DISPLAY "DEPTH ONLY " MAX-LEVEL
               END-IF
               DISPLAY "UNWOUND"
           END-IF
           GOBACK.
