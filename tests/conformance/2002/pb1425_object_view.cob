      *> kb/Work PB1425 - the object-view, ISO 8.4.3.1.2 identifier Format 5 (8.4.3.5):
      *>   identifier-1 AS { [FACTORY OF] object-class-name-1 [ONLY] | interface-name-1 | UNIVERSAL }
      *> 8.4.3.5.4 GR1: "This reference of identifier-1 is treated at compile-time as though it had the
      *> description specified by the AS phrase"; GR2-GR6 check the object at run time and set
      *> EC-OO-CONFORMANCE (Table 13: fatal, "Failure for an object-view") when it does not conform;
      *> GR7: "If UNIVERSAL is specified ... The EC-OO-CONFORMANCE exception condition is not set to
      *> exist." 14.8.1 NOTE 3: the object-view's rules "are checked at runtime if exception condition
      *> EC-OO-CONFORMANCE is enabled" - enabled here, so each failing view is handled by the
      *> declarative and RESUME AT NEXT STATEMENT continues. 8.4.3.1.4 GR1 c) applies the view before
      *> the inline invocation operator (e), so U AS PB1425VD :: "GETNAME" invokes on the view.
      *> Expected values (derived from the rules, not from a run):
      *>   SET OA TO OU AS PB1425VA         OU holds a PB1425VD, a subclass (GR2)   -> DERIVED
      *>   INVOKE OU AS PB1425VD "SPEAK"    the INVOKE receiver                     -> DERIVED
      *>   OU AS PB1425VD :: "GETNAME"      the inline receiver                     -> VD-OBJ
      *>   OU AS PB1425VD ONLY              exactly that class (GR4)                -> DERIVED
      *>   OU AS PB1425VI                   PB1425VD implements it, inherited (GR6) -> DERIVED
      *>   OD AS UNIVERSAL                  no check (GR7)                          -> DERIVED
      *>   OD AS UNIVERSAL AS PB1425VA      a view of a view (8.4.3.1.3 SR1)        -> DERIVED
      *>   OFU AS FACTORY OF PB1425VA       PB1425VD's factory, a subclass's (GR3)  -> DERIVED
      *>   OFU AS FACTORY OF PB1425VD ONLY  exactly PB1425VD's factory (GR5)        -> DERIVED
      *>   SELF AS PB1425VA in a method     SELF is of class object (SR1)           -> DERIVED
      *>   INVOKE OU AS UNIVERSAL "SPEAK"   the universal (dynamic) receiver        -> DERIVED
      *>   RAISE OU AS PB1425VA             14.9.29.3 SR2 asks the class (PB1197)   -> RAISED-OBJECT
      *>   a NULL reference viewed          no object to check                      -> NULL-VIEW
      *>   a PB1425VX viewed as PB1425VA    not a subclass (GR2)        -> HANDLED=EC-OO-CONFORMANCE
      *>   a PB1425VD viewed ... ONLY       not exactly the class (GR4) -> HANDLED=EC-OO-CONFORMANCE
      *>   a PB1425VX viewed as PB1425VI    does not implement it (GR6) -> HANDLED=EC-OO-CONFORMANCE
      *>   PB1425VD's factory viewed as FACTORY OF PB1425VA ONLY (GR5) -> HANDLED=EC-OO-CONFORMANCE
       >>TURN EC-OO-CONFORMANCE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425V.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425VA
           CLASS PB1425VD
           CLASS PB1425VX
           INTERFACE PB1425VI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OU  USAGE OBJECT REFERENCE.
       01 OU2 USAGE OBJECT REFERENCE.
       01 OA  USAGE OBJECT REFERENCE PB1425VA.
       01 OD  USAGE OBJECT REFERENCE PB1425VD.
       01 OI  USAGE OBJECT REFERENCE PB1425VI.
       01 OFU USAGE OBJECT REFERENCE.
       01 OFA USAGE OBJECT REFERENCE FACTORY OF PB1425VA.
       01 W   PIC X(8).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO-CONFORMANCE.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       R SECTION.
           USE AFTER EXCEPTION OBJECT PB1425VA.
       R-P.
           DISPLAY "RAISED-OBJECT".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE PB1425VD "NEW" RETURNING OD.
           SET OU TO OD.
           SET OA TO OU AS PB1425VA.
           INVOKE OA "SPEAK".
           INVOKE OU AS PB1425VD "SPEAK".
           MOVE OU AS PB1425VD :: "GETNAME" TO W.
           DISPLAY W.
           SET OD TO OU AS PB1425VD ONLY.
           INVOKE OD "SPEAK".
           SET OI TO OU AS PB1425VI.
           INVOKE OI "SPEAK".
           SET OU2 TO OD AS UNIVERSAL.
           INVOKE OU2 "SPEAK".
           SET OA TO OD AS UNIVERSAL AS PB1425VA.
           INVOKE OA "SPEAK".
           SET OFU TO PB1425VD.
           SET OFA TO OFU AS FACTORY OF PB1425VA.
           INVOKE OFA "NEW" RETURNING OA.
           INVOKE OA "SPEAK".
           SET OFA TO OFU AS FACTORY OF PB1425VD ONLY.
           INVOKE OFA "NEW" RETURNING OA.
           INVOKE OA "SPEAK".
           INVOKE OD "VIEWME".
           INVOKE OU AS UNIVERSAL "SPEAK".
           RAISE OU AS PB1425VA.
           SET OU TO NULL.
           SET OD TO OU AS PB1425VD.
           IF OD = NULL
               DISPLAY "NULL-VIEW"
           END-IF.
           INVOKE PB1425VX "NEW" RETURNING OU.
           SET OA TO OU AS PB1425VA.
           DISPLAY "AFTER-GR2".
           INVOKE PB1425VD "NEW" RETURNING OU.
           SET OA TO OU AS PB1425VA ONLY.
           DISPLAY "AFTER-GR4".
           INVOKE PB1425VX "NEW" RETURNING OU.
           SET OI TO OU AS PB1425VI.
           DISPLAY "AFTER-GR6".
           SET OFA TO OFU AS FACTORY OF PB1425VA ONLY.
           DISPLAY "AFTER-GR5".
           STOP RUN.
       END PROGRAM PB1425V.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB1425VI.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       END METHOD SPEAK.
       END INTERFACE PB1425VI.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425VA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE PB1425VI.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS PB1425VI.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
           DISPLAY "BASE".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS PB1425VA.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425VD INHERITS FROM PB1425VA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425VA.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK OVERRIDE.
       PROCEDURE DIVISION.
           DISPLAY "DERIVED".
       END METHOD SPEAK.
       METHOD-ID. GETNAME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-NAME PIC X(8).
       PROCEDURE DIVISION RETURNING LK-NAME.
           MOVE "VD-OBJ" TO LK-NAME.
       END METHOD GETNAME.
       METHOD-ID. VIEWME.
       PROCEDURE DIVISION.
           INVOKE SELF AS PB1425VA "SPEAK".
       END METHOD VIEWME.
       END OBJECT.
       END CLASS PB1425VD.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425VX INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS PB1425VX.
