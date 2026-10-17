*>--- conformance-case
*> id: pb366_use_exception_object_factory
*> rules: [GR-14.9.49.4-14]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB366A.
      *> ISO/IEC 1989:2023 §14.9.49.4 GR14 a): "If object-class-name-1 is
      *> specified and the exception object that was raised is a factory
      *> object or instance object of object-class-name-1 or of a subclass
      *> of object-class-name-1, the associated declarative is executed and
      *> no other declaratives are executed".  ONE clause, BOTH object
      *> kinds.  The five RAISEs below walk every arm of that sentence;
      *> §14.6.13.1.5 supplies the tail ("If there is no associated
      *> declarative, execution continues as specified in the RAISE
      *> statement"), so every arm prints its AFTER-n marker.  kb/Work PB366.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PBFBASE
           CLASS PBFSUB
           CLASS PBFKID
           CLASS PBFOTH
           CLASS PBFNON.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       OTH-SEC SECTION.
           USE AFTER EXCEPTION OBJECT PBFOTH.
       OTH-P.
           DISPLAY "OTH-HANDLER".
       SUB-SEC SECTION.
           USE AFTER EO PBFSUB.
       SUB-P.
           DISPLAY "SUB-HANDLER".
       BASE-SEC SECTION.
           USE AFTER EXCEPTION OBJECT PBFBASE.
       BASE-P.
           DISPLAY "BASE-HANDLER".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
      *> 1. SET U TO PBFBASE is the FACTORY object of PBFBASE (§14.9.39.3
      *> SR13).  OTH no; SUB no (a superclass is not a subclass); BASE
      *> matches on GR14a's "factory object ... of object-class-name-1".
           SET U TO PBFBASE.
           RAISE U.
           DISPLAY "AFTER-1".
      *> 2. The FACTORY object of PBFSUB matches SUB-SEC, and BASE-SEC also
      *> qualifies - GR14 selects by SOURCE ORDER and "no other
      *> declaratives are executed", so only SUB-HANDLER runs.
           SET U TO PBFSUB.
           RAISE U.
           DISPLAY "AFTER-2".
      *> 3. The FACTORY object of PBFKID, a subclass of PBFBASE that names
      *> no declarative of its own: GR14a's "or of a subclass of
      *> object-class-name-1", reached through a FACTORY object.
           SET U TO PBFKID.
           RAISE U.
           DISPLAY "AFTER-3".
      *> 4. An INSTANCE object of PBFSUB - the other half of GR14a's one
      *> clause, and the regression guard on the arm that already worked.
           INVOKE PBFSUB "NEW" RETURNING U.
           RAISE U.
           DISPLAY "AFTER-4".
      *> 5. The FACTORY object of PBFNON: no USE names PBFNON or a
      *> superclass of it, so no declarative is selected and §14.6.13.1.5
      *> continues at the statement after the RAISE.
           SET U TO PBFNON.
           RAISE U.
           DISPLAY "AFTER-5".
           STOP RUN.
       END PROGRAM PB366A.

       IDENTIFICATION DIVISION.
       CLASS-ID. PBFBASE INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS PBFBASE.

       IDENTIFICATION DIVISION.
       CLASS-ID. PBFSUB INHERITS FROM PBFBASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PBFBASE.
       END CLASS PBFSUB.

       IDENTIFICATION DIVISION.
       CLASS-ID. PBFKID INHERITS FROM PBFBASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PBFBASE.
       END CLASS PBFKID.

       IDENTIFICATION DIVISION.
       CLASS-ID. PBFOTH.
       END CLASS PBFOTH.

       IDENTIFICATION DIVISION.
       CLASS-ID. PBFNON.
       END CLASS PBFNON.
