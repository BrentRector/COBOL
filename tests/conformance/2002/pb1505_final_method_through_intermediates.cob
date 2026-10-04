      *> kb/Work PB1505 -- ISO 11.3.3 SR6, second sentence: "If the same
      *> method is inherited from one superclass through two or more
      *> intermediate superclasses, it may be specified with the FINAL
      *> clause."  PB1505W defines M1 FINAL; PB1505Z inherits it through
      *> the intermediates PB1505X and PB1505Y, which is legal.  (The
      *> first sentence concerns two DIFFERENT same-named methods
      *> inherited together, which only multiple class inheritance can
      *> produce -- Annex A.4.10 item 1, not claimed, COBOLNET0849.)
      *> Expected: INVOKE Z "M1" runs PB1505W's M1 (9.3.9: the subclass
      *> has all the methods of the inherited classes) -> W-M1-FINAL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1505M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1505Z.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Z USAGE OBJECT REFERENCE PB1505Z.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1505Z "NEW" RETURNING Z.
           INVOKE Z "M1".
           STOP RUN.
       END PROGRAM PB1505M.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1505W INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M1 FINAL.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "W-M1-FINAL".
       END METHOD M1.
       END OBJECT.
       END CLASS PB1505W.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1505X INHERITS FROM PB1505W.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1505W.
       END CLASS PB1505X.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1505Y INHERITS FROM PB1505X.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1505X.
       END CLASS PB1505Y.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1505Z INHERITS FROM PB1505Y.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1505Y.
       END CLASS PB1505Z.
