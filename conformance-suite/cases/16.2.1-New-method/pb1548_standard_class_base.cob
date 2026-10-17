*>--- conformance-case
*> id: pb1548_standard_class_base
*> rules: [GR-16.2.1.2-1, GR-16.2.2.2-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 clause 16 - THE STANDARD CLASS BASE (kb/Work PB1548, PB1506, PB1524).
      *> 16.1: "A standard class BASE shall be provided by the implementation. It may be used as the root of
      *>   a class hierarchy to provide standard object life-cycle function."      cite.py: OK 16.1
      *> 16.2: "The interface BaseFactoryInterface specifies the factory interface of the BASE class, and the
      *>   interface BaseInterface specifies the object interface of the BASE class." cite.py: OK 16.2
      *> 11.3.3 SR2: INHERITS FROM names "a class specified in the REPOSITORY paragraph of this source
      *>   element", and 12.3.8.3 SR6 b) needs "information in the external repository for the class" - the
      *>   implementation's standard class is always there, so CLASS BASE + INHERITS FROM BASE binds.
      *> 9.3.9: "The subclass has all the methods defined for the inherited class definition", so New (factory)
      *>   and FactoryObject (instance) are in PB1548A's and PB1548D's interfaces.
      *> 16.2.1.2 GR1: New "allocates storage for an object, initializes its instance data ... and returns a
      *>   reference to the created object".                               cite.py: OK 16.2.1.2 1)
      *> 16.2.2.2 GR1: FactoryObject "determines the class of the object and returns a reference to the
      *>   factory object associated with that class".                        cite.py: OK 16.2.2.2 1)
      *> DERIVATION, by output line:
      *>  1 INVOKE PB1548A "New": method-names compare case-insensitively (8.3.2.2); the new object's NAME
      *>    has its VALUE "A" (GR1's initialization)                                   -> SHOW A
      *>  2 an object of D in an A-typed reference: SHOW resolves on the runtime class D -> SHOW D
      *>  3 FactoryObject of that object is D's factory (GR1 "the class of the object"), WHO -> D-FACTORY
      *>  4 New through that FACTORY OF PB1548A reference runs on D's factory, so it creates a D
      *>    (9.3.14.3 "An instance object is created as the result of the NEW method being invoked on a
      *>    factory object")                                                            -> SHOW D
      *>  5 PB1548A's factory MAKE does INVOKE SELF "New": SELF is A's factory      -> SHOW A
      *>  6 the same MAKE inherited by D, invoked on D's factory: SELF is D's factory -> SHOW D
      *>  7 MYFAC (written in A, run on a D) does INVOKE SELF "FactoryObject"        -> D-FACTORY
      *>  8 a reference described BASE holds the D (a subclass conforms, 14.9.39.3 SR12 a)2.); its
      *>    FactoryObject, received in a universal reference, is D's factory; WHO through it -> D-FACTORY
      *>  9 New through that UNIVERSAL reference (the runtime dispatch of BASE's factory interface)
      *>    creates a D                                                                 -> SHOW D
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1548M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB1548A
           CLASS PB1548D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OA USAGE OBJECT REFERENCE PB1548A.
       01 OD USAGE OBJECT REFERENCE PB1548D.
       01 FA USAGE OBJECT REFERENCE FACTORY OF PB1548A.
       01 UB USAGE OBJECT REFERENCE BASE.
       01 U  USAGE OBJECT REFERENCE.
       01 U2 USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1548A "New" RETURNING OA
           INVOKE OA "SHOW"
           INVOKE PB1548D "NEW" RETURNING OD
           SET OA TO OD
           INVOKE OA "SHOW"
           INVOKE OA "FactoryObject" RETURNING FA
           INVOKE FA "WHO"
           INVOKE FA "NEW" RETURNING OA
           INVOKE OA "SHOW"
           INVOKE PB1548A "MAKE" RETURNING OA
           INVOKE OA "SHOW"
           INVOKE PB1548D "MAKE" RETURNING OA
           INVOKE OA "SHOW"
           INVOKE OD "MYFAC" RETURNING FA
           INVOKE FA "WHO"
           SET UB TO OD
           INVOKE UB "FactoryObject" RETURNING U
           INVOKE U "WHO"
           INVOKE U "New" RETURNING U2
           INVOKE U2 "SHOW"
           STOP RUN.
       END PROGRAM PB1548M.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1548A INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. WHO.
       PROCEDURE DIVISION.
           DISPLAY "A-FACTORY".
       END METHOD WHO.
       METHOD-ID. MAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-OBJ USAGE OBJECT REFERENCE PB1548A.
       PROCEDURE DIVISION RETURNING LK-OBJ.
           INVOKE SELF "New" RETURNING LK-OBJ.
       END METHOD MAKE.
       END FACTORY.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NAME PIC X VALUE "A".
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           DISPLAY "SHOW " NAME.
       END METHOD SHOW.
       METHOD-ID. MYFAC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-FAC USAGE OBJECT REFERENCE FACTORY OF PB1548A.
       PROCEDURE DIVISION RETURNING LK-FAC.
           INVOKE SELF "FactoryObject" RETURNING LK-FAC.
       END METHOD MYFAC.
       END OBJECT.
       END CLASS PB1548A.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1548D INHERITS FROM PB1548A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1548A.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. WHO OVERRIDE.
       PROCEDURE DIVISION.
           DISPLAY "D-FACTORY".
       END METHOD WHO.
       END FACTORY.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW OVERRIDE.
       PROCEDURE DIVISION.
           DISPLAY "SHOW D".
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB1548D.
