      *> reject-at: 2002 2014 2023
      *> kb/Work PB1582 - an override shall conform to the method it overrides.  ISO/IEC 1989:2023 section 11.7.3 SR3:
      *> "If the OVERRIDE phrase is specified, there shall be a method with the same method resolution signature as
      *> the method declared by this method definition defined in a superclass", and section 16.2's New has no USING
      *> phrase (its procedure division header is "Procedure division returning outObject."), so a NEW OVERRIDE that
      *> declares a USING parameter does not conform to BASE's New.
      *>   cite.py: OK  11.7.3 3)  /  OK  16.2 "Procedure division returning outObject."
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1582U INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. NEW OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-N PIC 9(3).
       01 LK USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION USING LK-N RETURNING LK.
       MAIN.
           INVOKE SUPER "NEW" RETURNING LK.
       END METHOD NEW.
       END FACTORY.
       END CLASS PB1582U.
