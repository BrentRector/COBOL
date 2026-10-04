      *> kb/Work PB1582 -- a factory METHOD-ID named NEW.
      *> ISO 16.1: "A standard class BASE shall be provided by the
      *> implementation. It may be used as the root of a class hierarchy
      *> ... This use is not required"; 16.2 declares New in BASE's
      *> factory interface (Method-id. New.) without the FINAL clause.
      *> So (a) in a class that does not inherit BASE, NEW is an ordinary
      *> factory method-name, and (b) in a BASE subclass NEW OVERRIDE is a
      *> legal override (11.7.3 SR3: "If the OVERRIDE phrase is specified,
      *> there shall be a method with the same method resolution signature
      *> ... defined in a superclass").  Expected values:
      *>   MY-NEW      PB1582R's own factory method runs
      *>   R=123       its RETURNING item, MOVE 123
      *>   OVR-NEW     INVOKE PB1582C "NEW" runs the override, which
      *>   C-HELLO     ... creates through SUPER "NEW" (BASE's New, 16.2.1.2
      *>               GR1) an object of the runtime factory's class, C
      *>   OVR-NEW     INVOKE PB1582D "NEW": D inherits C's override (9.3.9)
      *>   D-HELLO     ... and BASE's New creates a D, so D's HELLO runs
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1582P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1582R
           CLASS PB1582C
           CLASS PB1582D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(3).
       01 C USAGE OBJECT REFERENCE PB1582C.
       01 D USAGE OBJECT REFERENCE PB1582D.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1582R "NEW" RETURNING R.
           DISPLAY "R=" R.
           INVOKE PB1582C "NEW" RETURNING C.
           INVOKE C "HELLO".
           INVOKE PB1582D "NEW" RETURNING D.
           INVOKE D "HELLO".
           STOP RUN.
       END PROGRAM PB1582P.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1582R.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. NEW.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK PIC 9(3).
       PROCEDURE DIVISION RETURNING LK.
       MAIN.
           DISPLAY "MY-NEW".
           MOVE 123 TO LK.
       END METHOD NEW.
       END FACTORY.
       END CLASS PB1582R.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1582C INHERITS FROM BASE.
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
       01 LK USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING LK.
       MAIN.
           DISPLAY "OVR-NEW".
           INVOKE SUPER "NEW" RETURNING LK.
       END METHOD NEW.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. HELLO.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "C-HELLO".
       END METHOD HELLO.
       END OBJECT.
       END CLASS PB1582C.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1582D INHERITS FROM PB1582C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1582C.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. HELLO OVERRIDE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "D-HELLO".
       END METHOD HELLO.
       END OBJECT.
       END CLASS PB1582D.
