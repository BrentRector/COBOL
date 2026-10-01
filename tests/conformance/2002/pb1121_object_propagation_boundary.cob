      *> kb/Work PB1121 - the two checks on the exception-OBJECT
      *> propagation boundary.
      *>
      *> THE RULES.
      *> §14.6.13.1.5 item 1: if the exception object raised by a GOBACK
      *>   is neither "a) an object whose class is specified or whose
      *>   class is a subclass of a class specified in the RAISING
      *>   phrase of the procedure division header of the source element
      *>   containing this EXIT or GOBACK statement" nor "b) an object
      *>   that implements an interface specified" there, "execution of
      *>   the EXIT or GOBACK statement is as if EXCEPTION
      *>   EC-OO-EXCEPTION were specified in the RAISING phrase ...
      *>   instead of an exception object".
      *>   OK  §14.6.13.1.5 1)  (Exception objects)
      *> §14.6.13.1.5 item 4: with no declarative of the activating
      *>   element and no PROPAGATE ON, the GOBACK "is as if EXCEPTION
      *>   EC-OO-EXCEPTION were specified in the RAISING phrase, instead
      *>   of an exception object".
      *>   OK  §14.6.13.1.5 4)  (Exception objects)
      *> §14.9.18.4 GR1 b): a RAISING condition "is raised in the
      *>   activating runtime element if checking for that exception
      *>   condition is enabled in the activating runtime element".
      *>   OK  §14.9.18.4 1) b)  (General rules)
      *> §14.6.13.1.1: "By default, checking is not enabled for any
      *>   exception condition."
      *>   OK  §14.6.13.1.1  (General)
      *>
      *> DERIVATION.
      *> ARM1-AFTER -- item 4: the method BOOM of CPB1121S raises its
      *>   object (its header names the class, so item 1 is satisfied)
      *>   to an activator with no declaratives and no TURN. EC-OO-
      *>   EXCEPTION checking is off there, so the substituted condition
      *>   is not raised and the INVOKE completes: nothing is displayed
      *>   but the next statement.
      *> B-F4 / X-ECOO EC-OO-EXCEPTION / X-AFTER -- item 1: program
      *>   PB1121B has NO RAISING phrase in its header, so its F4
      *>   declarative's GOBACK RAISING LAST EXCEPTION of the CPB1121E
      *>   object is neither a) nor b); the GOBACK is as if EC-OO-
      *>   EXCEPTION, which PB1121X (checking ON, its own F4 declarative
      *>   present) raises. The F3 declarative for it runs (X-ECOO) and
      *>   RESUMEs; X's F4 declarative ("X-F4") must NOT run.
      *> C-F4 / Y-F4 / Y-AFTER -- the twin whose header says RAISING
      *>   CPB1121E: the object IS applicable, so it reaches the
      *>   activator's F4 declarative (item 2) and EC-OO-EXCEPTION is not
      *>   raised. Without this arm a fix that always converted would
      *>   pass the arm above.
      *> K-F4 / Z-F4 / Z-AFTER -- item 1 b): the header of PB1121K says
      *>   RAISING PB1121I and the object's class IMPLEMENTS PB1121I, so
      *>   it is applicable although no class is named.
      *> V-F4 / W-ECOO EC-OO-EXCEPTION / W-AFTER -- item 1 a)'s FACTORY
      *>   clause: the header of PB1121V says RAISING FACTORY OF
      *>   CPB1121E, and "the presence or absence of the FACTORY phrase
      *>   shall be the same" for the object reference raised; the
      *>   raised object is an INSTANCE, so FACTORY is absent there and
      *>   present in the header: not applicable, converted.
      *>   OK  §14.6.13.1.5 1) a)  (Exception objects)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1121M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB1121S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S USAGE OBJECT REFERENCE CPB1121S.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE CPB1121S "NEW" RETURNING S
           INVOKE S "BOOM"
           DISPLAY "ARM1-AFTER"
           CALL "PB1121X"
           CALL "PB1121Y"
           CALL "PB1121Z"
           CALL "PB1121W"
           DISPLAY "MAIN-END"
           STOP RUN.
       END PROGRAM PB1121M.

       >>TURN EC-OO-EXCEPTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1121X.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB1121E.
       PROCEDURE DIVISION.
       DECLARATIVES.
       X-F4-SEC SECTION.
           USE AFTER EXCEPTION OBJECT CPB1121E.
       X-F4-P.
           DISPLAY "X-F4".
       X-F3-SEC SECTION.
           USE AFTER EC EC-OO-EXCEPTION.
       X-F3-P.
           DISPLAY "X-ECOO " FUNCTION EXCEPTION-STATUS
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           CALL "PB1121B"
           DISPLAY "X-AFTER"
           GOBACK.
       END PROGRAM PB1121X.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1121B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB1121E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-E USAGE OBJECT REFERENCE CPB1121E.
       PROCEDURE DIVISION.
       DECLARATIVES.
       B-F4-SEC SECTION.
           USE AFTER EXCEPTION OBJECT CPB1121E.
       B-F4-P.
           DISPLAY "B-F4"
           GOBACK RAISING LAST EXCEPTION.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE CPB1121E "NEW" RETURNING W-E
           RAISE W-E.
       END PROGRAM PB1121B.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1121Y.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB1121E.
       PROCEDURE DIVISION.
       DECLARATIVES.
       Y-F4-SEC SECTION.
           USE AFTER EXCEPTION OBJECT CPB1121E.
       Y-F4-P.
           DISPLAY "Y-F4".
       Y-F3-SEC SECTION.
           USE AFTER EC EC-OO-EXCEPTION.
       Y-F3-P.
           DISPLAY "Y-ECOO-WRONG"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           CALL "PB1121C"
           DISPLAY "Y-AFTER"
           GOBACK.
       END PROGRAM PB1121Y.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1121C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB1121E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-E USAGE OBJECT REFERENCE CPB1121E.
       PROCEDURE DIVISION RAISING CPB1121E.
       DECLARATIVES.
       C-F4-SEC SECTION.
           USE AFTER EXCEPTION OBJECT CPB1121E.
       C-F4-P.
           DISPLAY "C-F4"
           GOBACK RAISING LAST EXCEPTION.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE CPB1121E "NEW" RETURNING W-E
           RAISE W-E.
       END PROGRAM PB1121C.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1121Z.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB1121E.
       PROCEDURE DIVISION.
       DECLARATIVES.
       Z-F4-SEC SECTION.
           USE AFTER EXCEPTION OBJECT CPB1121E.
       Z-F4-P.
           DISPLAY "Z-F4".
       Z-F3-SEC SECTION.
           USE AFTER EC EC-OO-EXCEPTION.
       Z-F3-P.
           DISPLAY "Z-ECOO-WRONG"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           CALL "PB1121K"
           DISPLAY "Z-AFTER"
           GOBACK.
       END PROGRAM PB1121Z.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1121K.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB1121E
           INTERFACE PB1121I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-E USAGE OBJECT REFERENCE CPB1121E.
       PROCEDURE DIVISION RAISING PB1121I.
       DECLARATIVES.
       K-F4-SEC SECTION.
           USE AFTER EXCEPTION OBJECT CPB1121E.
       K-F4-P.
           DISPLAY "K-F4"
           GOBACK RAISING LAST EXCEPTION.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE CPB1121E "NEW" RETURNING W-E
           RAISE W-E.
       END PROGRAM PB1121K.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1121W.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB1121E.
       PROCEDURE DIVISION.
       DECLARATIVES.
       W-F4-SEC SECTION.
           USE AFTER EXCEPTION OBJECT CPB1121E.
       W-F4-P.
           DISPLAY "W-F4".
       W-F3-SEC SECTION.
           USE AFTER EC EC-OO-EXCEPTION.
       W-F3-P.
           DISPLAY "W-ECOO " FUNCTION EXCEPTION-STATUS
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           CALL "PB1121V"
           DISPLAY "W-AFTER"
           GOBACK.
       END PROGRAM PB1121W.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1121V.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB1121E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-E USAGE OBJECT REFERENCE CPB1121E.
       PROCEDURE DIVISION RAISING FACTORY OF CPB1121E.
       DECLARATIVES.
       V-F4-SEC SECTION.
           USE AFTER EXCEPTION OBJECT CPB1121E.
       V-F4-P.
           DISPLAY "V-F4"
           GOBACK RAISING LAST EXCEPTION.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE CPB1121E "NEW" RETURNING W-E
           RAISE W-E.
       END PROGRAM PB1121V.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB1121I.
       END INTERFACE PB1121I.

       IDENTIFICATION DIVISION.
       CLASS-ID. CPB1121E INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE PB1121I.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS PB1121I.
       END OBJECT.
       END CLASS CPB1121E.

       IDENTIFICATION DIVISION.
       CLASS-ID. CPB1121S INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS CPB1121E.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-E USAGE OBJECT REFERENCE CPB1121E.
       PROCEDURE DIVISION.
       METHOD-ID. BOOM.
       PROCEDURE DIVISION RAISING CPB1121E.
       MAIN.
           INVOKE CPB1121E "NEW" RETURNING W-E
           GOBACK RAISING W-E.
       END METHOD BOOM.
       END OBJECT.
       END CLASS CPB1121S.
