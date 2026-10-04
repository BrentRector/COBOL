      *> kb/Work PB1425 - the qualified predefined object reference `object-class-name-1 OF SUPER`.
      *>
      *> THE RULE. ISO/IEC 1989:2023 8.4.3.8.2 prints { SELF | [ object-class-name-1 OF ] SUPER } (SELF, OF, SUPER
      *> underlined). 8.4.3.8.3 SR6: "If the INHERITS clause of the containing class definition specifies only one
      *> object-class-name, object-class-name-1 may be specified"; SR4: it "shall be the name of a class specified in
      *> the INHERITS clause of the containing class definition"; 8.4.3.8.4 GR4: "If object-class-name-1 is
      *> specified, the search for the method shall include only those methods defined for object-class-name-1".
      *> The INVOKE statement and the inline invocation share the receiver (8.4.3.4.4 GR1), so both spellings appear.
      *>
      *> THE CLASSES. PB1425A defines SPEAK and NAME. PB1425B INHERITS FROM PB1425A and overrides SPEAK. PB1425C
      *> INHERITS FROM PB1425B and overrides SPEAK again; its SPEAK writes PB1425B OF SUPER, the class its own
      *> INHERITS clause names.
      *>
      *> EXPECTED OUTPUT (the program makes a PB1425C and invokes SPEAK on it):
      *>   ANIMAL     PB1425C's SPEAK -> INVOKE PB1425B OF SUPER "SPEAK": the search covers the methods defined for
      *>   DOG        PB1425B (GR4), so PB1425B's SPEAK runs; it does INVOKE SUPER "SPEAK" (GR3: PB1425A's SPEAK,
      *>              ANIMAL) and then displays DOG.
      *>   PUPPY      PB1425C's SPEAK then displays PUPPY.
      *>   A-NAME     PB1425B OF SUPER :: "NAME" - an inline invocation through the qualified SUPER; NAME is defined
      *>              for PB1425B by inheritance from PB1425A (9.3.9: "The subclass has all the methods defined for
      *>              the inherited class definition"), and returns "A-NAME".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OC USAGE OBJECT REFERENCE PB1425C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1425C "NEW" RETURNING OC
           INVOKE OC "SPEAK"
           STOP RUN.
       END PROGRAM PB1425M.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425A INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
           DISPLAY "ANIMAL".
       END METHOD SPEAK.
       METHOD-ID. NAME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC X(6).
       PROCEDURE DIVISION RETURNING LN.
           MOVE "A-NAME" TO LN.
       END METHOD NAME.
       END OBJECT.
       END CLASS PB1425A.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425B INHERITS FROM PB1425A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425A.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK OVERRIDE.
       PROCEDURE DIVISION.
           INVOKE SUPER "SPEAK".
           DISPLAY "DOG".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS PB1425B.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425C INHERITS FROM PB1425B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1425B.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK OVERRIDE.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 WN PIC X(6).
       PROCEDURE DIVISION.
           INVOKE PB1425B OF SUPER "SPEAK".
           DISPLAY "PUPPY".
           MOVE PB1425B OF SUPER :: "NAME" TO WN.
           DISPLAY WN.
       END METHOD SPEAK.
       END OBJECT.
       END CLASS PB1425C.
