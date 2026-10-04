      *> reject-at: 2002 2014 2023
      *> kb/Work PB1582 - ISO/IEC 1989:2023 section 11.7.3 SR4 a): "if this method definition is contained in a class
      *> definition, no inherited method shall have the same method resolution signature as the method declared by
      *> this method definition".  Section 16.2 declares New in the factory interface of the standard class BASE, so
      *> a BASE subclass inherits it and may redefine it only with OVERRIDE.
      *>   cite.py: OK  11.7.3 4) a)  /  OK  16.2 "Method-id. New."
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1582W INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. NEW.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING LK.
       MAIN.
           INVOKE SUPER "NEW" RETURNING LK.
       END METHOD NEW.
       END FACTORY.
       END CLASS PB1582W.
