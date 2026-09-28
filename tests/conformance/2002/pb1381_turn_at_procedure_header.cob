      *> ISO §7.3.25.4 GR6/GR8 - a >>TURN written between an activated
      *> element's DATA DIVISION and its PROCEDURE DIVISION header
      *> governs that header, so it decides the activated half of the
      *> "enabled in both" parameter-conformance gates (kb/Work PB1381)
      *> RULE §7.3.25.4 GR6: "checking for the exception condition
      *>   associated with exception-name-1 is enabled for the procedure
      *>   division statements and procedure division headers that
      *>   follow in the compilation group".
      *>   cite.py --check 7.3.25.4 "procedure division statements and
      *>   procedure division headers that follow in the compilation
      *>   group" -> OK §7.3.25.4 6)
      *> RULE §7.3.25.4 GR8: the OFF phrase disables checking "for all
      *>   procedure division statements and procedure division headers
      *>   that follow in the compilation group".
      *>   cite.py --check 7.3.25.4 "checking for the exception condition
      *>   associated with exception-name-1 is disabled for all
      *>   procedure division statements and procedure division
      *>   headers" -> OK §7.3.25.4 8)
      *> RULE §14.9.4.4 GR3 d): EC-PROGRAM-ARG-MISMATCH "is set to exist
      *>   if checking for it is enabled in both the activated program
      *>   and activating runtime element".
      *>   cite.py --check 14.9.4.4 "is set to exist if checking for it
      *>   is enabled in both the activated program and activating
      *>   runtime element" -> OK §14.9.4.4 3)
      *> RULE §14.9.23.4 GR7 c): the same "enabled in both the activated
      *>   method and the activating runtime element" gate for
      *>   EC-OO-UNIVERSAL (cite.py --check 14.9.23.4 "is set to exist if
      *>   checking for it is enabled in both the activated method and
      *>   the activating runtime element" -> OK §14.9.23.4 7)).
      *>   GR7 c) is reached only by a BOUND method: GR7 b) resolves
      *>   first by §9.3.6, whose match rules require a BY REFERENCE
      *>   argument to have the formal's PICTURE (cite.py --check 9.3.6
      *>   "has the same ALIGNED, ANY LENGTH, BLANK WHEN ZERO, DYNAMIC
      *>   LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses" -> OK,
      *>   labelled 3) d) 5. - the transcription nests e) under d)), so
      *>   a USING descriptor mismatch matches NO method and is §9.3.6
      *>   6)'s EC-OO-METHOD ("otherwise, the EC-OO-METHOD exception
      *>   condition is set to exist" -> OK §9.3.6 6)). A RETURNING
      *>   item only has to be a MOVE receiver (§9.3.6 7) -> OK), so a
      *>   PIC 9(6) receiver BINDS a PIC 9(4) returning method, and
      *>   §14.8.3.3's same-PICTURE conformance ("the receiving operand
      *>   shall have the same ALIGN, BLANK WHEN ZERO, DYNAMIC LENGTH,
      *>   JUSTIFIED, PICTURE, SIGN, and USAGE clauses" -> OK §14.8.3.3)
      *>   is the violation GR7 c) names.
      *> §14.8.4.1's "before the Environment division" fold point is
      *>   the EC-EXTERNAL conditions' alone; it is not these rules'.
      *> SET-UP: line 1 enables both conditions, so every activating
      *>   statement of PB1381P is enabled. The CALLs pass TWO
      *>   arguments to one-formal programs (a count violation); the
      *>   INVOKE receives TAKE's PIC 9(4) returning item into PIC 9(6)
      *>   through a universal object reference (bound by §9.3.6 7),
      *>   a §14.8.3.3 returning-item conformance violation).
      *>   PB1381A: EC-ALL OFF before its IDENTIFICATION DIVISION, then
      *>     EC-PROGRAM-ARG-MISMATCH ON just before its header.
      *>   PB1381B: EC-PROGRAM-ARG-MISMATCH OFF just before its header.
      *>   Method TAKE: EC-OO-UNIVERSAL ON just before its header (the
      *>     state at its METHOD-ID line is OFF, from PB1381A's EC-ALL).
      *>   (A method whose header is DISABLED has no golden leg: with
      *>   the condition not enabled in both, a nonconforming universal
      *>   crossing stops the run unit as an implementor-defined fatal
      *>   error - OoEmitter.OoUnivStop.)
      *> EXPECTED OUTPUT, DERIVED:
      *>   A EXC [EC-PROGRAM-ARG-MISMATCH] (the name padded to the
      *>        31-character EXCEPTION-STATUS value): enabled at
      *>        PB1381A's header and at the CALL - set, and the call
      *>        is not successful.
      *>   IN-B / B OK: disabled at PB1381B's header - not set, the
      *>        call succeeds.
      *>   HANDLED=EC-OO-UNIVERSAL / C AFTER: enabled at TAKE's
      *>        header and at the INVOKE - set, the declarative runs
      *>        and RESUME AT NEXT STATEMENT continues.
      >>TURN EC-PROGRAM-ARG-MISMATCH EC-OO-UNIVERSAL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1381P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1381C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4) VALUE "AAAA".
       01 PA PIC X(8) VALUE "PB1381A".
       01 PB PIC X(8) VALUE "PB1381B".
       01 O USAGE OBJECT REFERENCE.
       01 C USAGE OBJECT REFERENCE PB1381C.
       01 W PIC 9(6) VALUE 000007.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO-UNIVERSAL.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           CALL PA USING A A
             ON EXCEPTION
               DISPLAY "A EXC [" FUNCTION EXCEPTION-STATUS
                   "]"
             NOT ON EXCEPTION DISPLAY "A OK"
           END-CALL
           CALL PB USING A A
             ON EXCEPTION
               DISPLAY "B EXC [" FUNCTION EXCEPTION-STATUS
                   "]"
             NOT ON EXCEPTION DISPLAY "B OK"
           END-CALL
           INVOKE PB1381C "NEW" RETURNING C
           SET O TO C
           INVOKE O "TAKE" RETURNING W
           DISPLAY "C AFTER"
           STOP RUN.
       END PROGRAM PB1381P.
       >>TURN EC-ALL CHECKING OFF
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1381A.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC X(4).
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       PROCEDURE DIVISION USING X.
       P-MAIN.
           DISPLAY "IN-A"
           GOBACK.
       END PROGRAM PB1381A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1381B.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC X(4).
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING OFF
       PROCEDURE DIVISION USING X.
       P-MAIN.
           DISPLAY "IN-B"
           GOBACK.
       END PROGRAM PB1381B.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1381C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK PIC 9(4).
       >>TURN EC-OO-UNIVERSAL CHECKING ON
       PROCEDURE DIVISION RETURNING LK.
       MAIN-P.
           DISPLAY "IN-TAKE".
       END METHOD TAKE.
       END OBJECT.
       END CLASS PB1381C.
