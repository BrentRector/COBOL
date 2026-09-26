      *> ISO §14.9.4.4 GR3 b)/c) (Annex A.1 item 14) — the runtime
      *> resources a CALL checks: the documented set is EMPTY, so a
      *> module file that exists but cannot be loaded is "not located"
      *> (EC-PROGRAM-NOT-FOUND) and EC-PROGRAM-RESOURCES is never raised.
      *>   cite.py --check 14.9.4.4 "The runtime resources that are
      *>     checked in order to determine the availability of the called
      *>     program for execution are defined by the implementor."
      *>     -> OK 3) c)
      *>   cite.py --check 14.9.4.4 "If the program is located but the
      *>     resources necessary to execute the program are not available,
      *>     the EC-PROGRAM-RESOURCES exception condition is set to exist"
      *>     -> OK 3) c)
      *>   cite.py --check 14.9.4.4 "If the program cannot be located or
      *>     identifier-1 references a zero-length item, the
      *>     EC-PROGRAM-NOT-FOUND exception condition is set to exist."
      *>     -> OK 3) b)
      *>   cite.py --check 14.9.4.4 "If the exception condition is any of
      *>     the EC-PROGRAM or EC-EXTERNAL exception conditions and an ON
      *>     EXCEPTION phrase is specified in the CALL statement, control
      *>     is transferred to imperative-statement-1." -> OK 3) h) 1.
      *>   cite.py --check 14.9.4.4 "If checking for the exception
      *>     condition is enabled, and if the exception condition is one
      *>     of the EC-PROGRAM or EC-EXTERNAL exception conditions and an
      *>     ON EXCEPTION phrase is not specified" -> OK 3) h) 2.
      *>   cite.py --check 14.6.13.1.3 "If checking for the exception
      *>     condition is enabled and there is an applicable USE statement
      *>     in the source unit that specifies the exception-name
      *>     associated with the exception condition" -> OK 5)
      *>   cite.py --check 14.9.33.4 "the implicit CONTINUE statement
      *>     immediately follows the end of the statement that was
      *>     executing when control was transferred to the exception
      *>     processing procedure" -> OK 2) a)
      *>   cite.py --check 15.33.3 "A 31-character, left-justified,
      *>     alphanumeric character string that is the exception-name"
      *>     -> OK 1)
      *> THE DETERMINATION PINNED (docs/CONFORMANCE.md DOC-A.1-14): no
      *> runtime resource is checked beyond locating the program; a
      *> callee is located when it is registered - compiled into the run
      *> unit, or a separately compiled module <name>.dll in the
      *> application directory whose registrar has run. A module file
      *> that exists but cannot be loaded has therefore NOT been located:
      *> GR3 b)'s arm, EC-PROGRAM-NOT-FOUND - never EC-PROGRAM-RESOURCES.
      *> THE SHAPE: the program first WRITES a file L1CRSMD.dll holding
      *> text that is not a loadable image into its own directory (the
      *> assign target carries a '.', so it names that file in the
      *> current directory, which is the application directory the
      *> program runs from), then CALLs "L1CRSMD". Checking for every
      *> EC-PROGRAM condition is enabled, and BOTH candidate exceptions
      *> have a declarative, so whichever the CALL raises is visible.
      *> DERIVATION of every .out line:
      *>   SUB-RAN / SUB-CALLED   control: a located COBOL program is
      *>       called with checking on; no resource check refuses it
      *>       (GR3 c) with an empty set) and NOT ON EXCEPTION runs (GR3 i)).
      *>   PLANTED 00   the WRITE of the stand-in module succeeded.
      *>   CALL-1 NOT-FOUND EC-PROGRAM-NOT-FOUND   the file exists but no
      *>       program is registered from it, so the program is not
      *>       located (GR3 b)); ON EXCEPTION runs (GR3 h) 1.) and the
      *>       last exception status names the condition (15.33.3).
      *>   DECL-NF EC-PROGRAM-NOT-FOUND   the same CALL with no ON
      *>       EXCEPTION phrase: checking is enabled, so the applicable
      *>       declarative runs (GR3 h) 2.; 14.6.13.1.3 5)) - the
      *>       NOT-FOUND one, never the RESOURCES one (no DECL-RES line).
      *>   END   RESUME AT NEXT STATEMENT continues after that CALL
      *>       (14.9.33.4 2) a)).
       >>TURN EC-PROGRAM CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CRS01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MODF ASSIGN TO "L1CRSMD.dll"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS MOD-FS.
       DATA DIVISION.
       FILE SECTION.
       FD  MODF.
       01  MOD-REC PIC X(24).
       WORKING-STORAGE SECTION.
       01  MOD-FS  PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       NF-SECT SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM-NOT-FOUND.
       NF-PARA.
           DISPLAY "DECL-NF " FUNCTION EXCEPTION-STATUS
           RESUME AT NEXT STATEMENT.
       RES-SECT SECTION.
           USE AFTER EXCEPTION CONDITION EC-PROGRAM-RESOURCES.
       RES-PARA.
           DISPLAY "DECL-RES " FUNCTION EXCEPTION-STATUS
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           CALL "L1CRSSUB"
               ON EXCEPTION DISPLAY "SUB-NOT-FOUND"
               NOT ON EXCEPTION DISPLAY "SUB-CALLED"
           END-CALL
           OPEN OUTPUT MODF
           MOVE "NOT A LOADABLE IMAGE" TO MOD-REC
           WRITE MOD-REC
           CLOSE MODF
           DISPLAY "PLANTED " MOD-FS
           CALL "L1CRSMD"
               ON EXCEPTION
                   DISPLAY "CALL-1 NOT-FOUND " FUNCTION EXCEPTION-STATUS
               NOT ON EXCEPTION
                   DISPLAY "CALL-1 CALLED"
           END-CALL
           CALL "L1CRSMD"
           DISPLAY "END"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CRSSUB.
       PROCEDURE DIVISION.
       SUB-PARA.
           DISPLAY "SUB-RAN"
           GOBACK.
       END PROGRAM L1CRSSUB.
       END PROGRAM L1CRS01.
