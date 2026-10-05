      *> kb/Work PB1425 - an object-view with EC-OO-CONFORMANCE NOT enabled. 14.8.1 NOTE 3: the
      *> object-view's run-time rules "are checked at runtime if exception condition EC-OO-CONFORMANCE is
      *> enabled"; no >>TURN enables it here, so 8.4.3.5.4 GR4's exact-class failure sets nothing and the
      *> view proceeds: OU references a PB1425UD, a subclass of PB1425UA, so `OU AS PB1425UA ONLY` does not
      *> conform (GR4: "not an object of object-class-name-1") but the object is still a PB1425UA and
      *> the program continues with it. Expected: DERIVED, then AFTER (no declarative, no termination).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425U.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425UA
           CLASS PB1425UD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OU USAGE OBJECT REFERENCE.
       01 OA USAGE OBJECT REFERENCE PB1425UA.
       PROCEDURE DIVISION.
           INVOKE PB1425UD "NEW" RETURNING OU.
           SET OA TO OU AS PB1425UA ONLY.
           INVOKE OA "SPEAK".
           DISPLAY "AFTER".
           STOP RUN.
       END PROGRAM PB1425U.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425UA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
           DISPLAY "BASE".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS PB1425UA.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425UD INHERITS FROM PB1425UA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425UA.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK OVERRIDE.
       PROCEDURE DIVISION.
           DISPLAY "DERIVED".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS PB1425UD.
