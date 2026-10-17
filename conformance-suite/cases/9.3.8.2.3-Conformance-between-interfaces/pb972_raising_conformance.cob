*>--- conformance-case
*> id: pb972_raising_conformance
*> rules: [GR-9.3.8.2.3-9, SR-11.7.3-9]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB972 - ISO 9.3.8.2.3 rule 9, the CONFORMING shapes.
      *> "If the RAISING phrase is specified in the procedure division
      *> header of the method in interface-1, the corresponding method in
      *> interface-2 specifies the RAISING phrase following these rules":
      *>  a) an exception-name - the same exception-name (W4);
      *>  b) an object-class-name - the same object-class-name or the
      *>     name of a superclass of the class identified by that
      *>     object-class-name (W1: the class raises PB972E1, the
      *>     prototype lists its superclass PB972E0), or the name of an
      *>     interface implemented by the instance object of that class
      *>     (W2: PB972E1's OBJECT paragraph IMPLEMENTS PB972IX);
      *>  c) an interface-name - the same interface-name or the name of an
      *>     interface inherited by that interface (W3: the class raises
      *>     PB972IB, which INHERITS the listed PB972IA).
      *> The rule constrains interface-2 only where interface-1 HAS a
      *> RAISING element, so W5 (the class raises nothing, the prototype
      *> lists PB972E0) conforms too. 9.3.11 makes the implementing class
      *> conform to its interface (the class is interface-1); 11.7.3 SR9
      *> makes an override conform to the method it overrides (the
      *> override is interface-1): PB972SUB's M raises PB972E1 where
      *> PB972BASE's M lists PB972E0, its superclass - conforming.
      *> Every method runs and returns normally; the output is the trace.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB972IX.
       END INTERFACE PB972IX.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB972IA.
       END INTERFACE PB972IA.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB972IB INHERITS PB972IA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB972IA.
       END INTERFACE PB972IB.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB972E0.
       END CLASS PB972E0.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB972E1 INHERITS PB972E0.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB972E0
           INTERFACE PB972IX.
       IDENTIFICATION DIVISION.
       OBJECT.
       IMPLEMENTS PB972IX.
       END OBJECT.
       END CLASS PB972E1.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB972IW.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB972E0
           INTERFACE PB972IX
           INTERFACE PB972IA.
       PROCEDURE DIVISION.
       METHOD-ID. W1.
       PROCEDURE DIVISION RAISING PB972E0.
       END METHOD W1.
       METHOD-ID. W2.
       PROCEDURE DIVISION RAISING PB972IX.
       END METHOD W2.
       METHOD-ID. W3.
       PROCEDURE DIVISION RAISING PB972IA.
       END METHOD W3.
       METHOD-ID. W4.
       PROCEDURE DIVISION RAISING EC-USER-PB972.
       END METHOD W4.
       METHOD-ID. W5.
       PROCEDURE DIVISION RAISING PB972E0.
       END METHOD W5.
       END INTERFACE PB972IW.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB972C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB972E1
           INTERFACE PB972IB
           INTERFACE PB972IW.
       IDENTIFICATION DIVISION.
       OBJECT.
       IMPLEMENTS PB972IW.
       PROCEDURE DIVISION.
       METHOD-ID. W1.
       PROCEDURE DIVISION RAISING PB972E1.
           DISPLAY "W1".
       END METHOD W1.
       METHOD-ID. W2.
       PROCEDURE DIVISION RAISING PB972E1.
           DISPLAY "W2".
       END METHOD W2.
       METHOD-ID. W3.
       PROCEDURE DIVISION RAISING PB972IB.
           DISPLAY "W3".
       END METHOD W3.
       METHOD-ID. W4.
       PROCEDURE DIVISION RAISING EC-USER-PB972.
           DISPLAY "W4".
       END METHOD W4.
       METHOD-ID. W5.
       PROCEDURE DIVISION.
           DISPLAY "W5".
       END METHOD W5.
       END OBJECT.
       END CLASS PB972C.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB972BASE INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB972E0.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       PROCEDURE DIVISION RAISING PB972E0.
           DISPLAY "BASE M".
       END METHOD M.
       END OBJECT.
       END CLASS PB972BASE.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB972SUB INHERITS PB972BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB972BASE
           CLASS PB972E1.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M OVERRIDE.
       PROCEDURE DIVISION RAISING PB972E1.
           DISPLAY "SUB M".
       END METHOD M.
       END OBJECT.
       END CLASS PB972SUB.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB972MAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB972C
           CLASS PB972SUB
           CLASS PB972BASE
           INTERFACE PB972IW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WI USAGE OBJECT REFERENCE PB972IW.
       01 WB USAGE OBJECT REFERENCE PB972BASE.
       PROCEDURE DIVISION.
           INVOKE PB972C "NEW" RETURNING WI
           INVOKE WI "W1"
           INVOKE WI "W2"
           INVOKE WI "W3"
           INVOKE WI "W4"
           INVOKE WI "W5"
           INVOKE PB972SUB "NEW" RETURNING WB
           INVOKE WB "M"
           STOP RUN.
       END PROGRAM PB972MAIN.
