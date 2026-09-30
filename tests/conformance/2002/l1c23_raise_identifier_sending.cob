      *> ISO §14.9.29.3 3) — RAISE identifier-1 is a SENDING operand
      *> Rule: "Identifier-1 is a sending operand."
      *>   cite.py: OK  §14.9.29.3 3)  (Syntax rules)
      *> Consequence exercised: identifier-1 may be an object reference that the standard forbids as a
      *> RECEIVING operand, and RAISE only reads it:
      *>   §8.4.3.8.3 2) (SELF) "This identifier format shall not be specified as a receiving operand."
      *>     cite.py: OK  §8.4.3.8.3 2)  (Syntax rules)
      *>   §8.4.3.6.3 1) "EXCEPTION-OBJECT shall not be specified as a receiving operand."
      *>     cite.py: OK  §8.4.3.6.3 1)  (Syntax rules)
      *> Semantics used for the expected lines:
      *>   §14.9.29.4 2) "If identifier-1 is specified, EXCEPTION-OBJECT is set to reference the object
      *>   referenced by identifier-1. If there is no applicable declarative, processing continues with
      *>   the statement following the RAISE statement."   cite.py: OK  §14.9.29.4 2)  (General rules)
      *>   §8.4.3.6.4 1) "EXCEPTION-OBJECT references the current exception object."
      *>     cite.py: OK  §8.4.3.6.4 1)  (General rules)
      *> The object is L1C23Y's factory object (SELF in a factory method), so no instance creation is
      *> needed.  No declarative is declared anywhere, so each RAISE continues with the next statement.
      *> EXPECTED OUTPUT, derived:
      *>   EO-IS-SELF     RAISE SELF (legal only because identifier-1 is sending) sets EXCEPTION-OBJECT
      *>                  to the object SELF references, which O was set to beforehand.
      *>   EO-STILL-SELF  RAISE EXCEPTION-OBJECT (legal only because identifier-1 is sending) sets
      *>                  EXCEPTION-OBJECT to the object EXCEPTION-OBJECT references -- the same object.
      *>   O-INTACT       O, a sending operand of the third RAISE, still references that object.
      *>   BACK           control returns to the invoking program after the method's GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C23M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C23Y.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE L1C23Y "RAISER"
           DISPLAY "BACK"
           STOP RUN.
       END PROGRAM L1C23M.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C23Y.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. RAISER.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01  O USAGE OBJECT REFERENCE.
       01  O2 USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
           SET O TO SELF
           RAISE SELF
           IF EXCEPTION-OBJECT = O
               DISPLAY "EO-IS-SELF"
           ELSE
               DISPLAY "EO-NOT-SELF"
           END-IF
           RAISE EXCEPTION-OBJECT
           IF EXCEPTION-OBJECT = O
               DISPLAY "EO-STILL-SELF"
           ELSE
               DISPLAY "EO-CHANGED"
           END-IF
           RAISE O
           SET O2 TO SELF
           IF O = O2
               DISPLAY "O-INTACT"
           ELSE
               DISPLAY "O-CHANGED"
           END-IF
           GOBACK.
       END METHOD RAISER.
       END FACTORY.
       END CLASS L1C23Y.
