*>--- conformance-case
*> id: pb892_performed_declarative_resume
*> rules: [GR-14.9.33.4-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB892 - RESUME in a declarative that a PERFORM in the
      *> NONDECLARATIVE portion executed (ISO 14.9.49.3 SR4 admits the
      *> reference) rather than an exception condition. ISO 14.9.33.4
      *> GR2 b): "If the declarative was not executed because of an
      *> exception condition but was executed instead by a PERFORM
      *> statement in the nondeclarative portion of the source element
      *> that referenced the declarative procedure, the implicit CONTINUE
      *> statement immediately follows the last statement of the
      *> terminating procedure referenced in that PERFORM statement".
      *> GR3: RESUME AT procedure-name is "as if a GO TO procedure-name-1
      *> were executed".
      *>
      *> C1 PERFORM HZ-P 2 TIMES: each RESUME NEXT ends that execution of
      *>    HZ-P, the TIMES phrase goes on - DECL 1, DECL 2, AFTER-TIMES 2.
      *> C2 PERFORM HZ (the section): the terminating procedure is HZ, so
      *>    control continues after HZ's LAST statement - HZ-Q never shows.
      *> C3 PERFORM HZ-P with K = 4: RESUME AT TAIL-P is a GO TO - the
      *>    PERFORM is abandoned and NOT-REACHED never shows.
      *> C4 in TAIL-P the same declarative is entered by an EXCEPTION
      *>    (RAISE): RESUME NEXT continues after the RAISE (GR2 a) 1.).
      *> Before PB892 the performed declarative's RESUME escaped every
      *> frame and the run unit died with an unhandled .NET exception.
       >>TURN EC-USER-RZ CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB892PR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HZ SECTION. USE AFTER EXCEPTION CONDITION EC-USER-RZ.
       HZ-P.
           ADD 1 TO K.
           DISPLAY "DECL " K.
           IF K = 4
               RESUME AT TAIL-P
           END-IF.
           RESUME AT NEXT STATEMENT.
           DISPLAY "NEVER".
       HZ-Q.
           DISPLAY "HZ-Q " K.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           PERFORM HZ-P 2 TIMES.
           DISPLAY "AFTER-TIMES " K.
           PERFORM HZ.
           DISPLAY "AFTER-SECTION " K.
           PERFORM HZ-P.
           DISPLAY "NOT-REACHED".
           STOP RUN.
       TAIL-P.
           DISPLAY "TAIL " K.
           RAISE EXCEPTION EC-USER-RZ.
           DISPLAY "AFTER-RAISE " K.
           STOP RUN.
