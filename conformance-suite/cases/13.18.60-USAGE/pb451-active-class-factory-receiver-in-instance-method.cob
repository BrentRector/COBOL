*>--- conformance-case
*> id: pb451-active-class-factory-receiver-in-instance-method
*> rules: [GR-13.18.60.4-22, SR-14.9.39.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 14 b)2.: with identifier-3 described with an ACTIVE-CLASS
      *> phrase and the sender "the predefined object SELF", "if the data item referenced by
      *> identifier-3 is described with a FACTORY phrase, the method containing the SET
      *> statement shall be defined in the factory definition of its containing class."
      *> §13.18.60.4 GR22 e)1.: with FACTORY the item shall hold "the factory object of that
      *> class", and SELF in an instance method is an instance object.  The conforming twin is
      *> the FMK method of conformance:2002/pb389_object_reference_descriptor (SET FA TO SELF
      *> inside a FACTORY method, prints PING-D).

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB451N8.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB451N8C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB451N8C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB451N8C "NEW" RETURNING O.
           INVOKE O "MK".
           STOP RUN.
       END PROGRAM PB451N8.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451N8C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. TAG.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FTAG".
       END METHOD TAG.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WHO.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "WHO".
       END METHOD WHO.
       METHOD-ID. MK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 R USAGE OBJECT REFERENCE FACTORY OF ACTIVE-CLASS.
       PROCEDURE DIVISION.
       MAIN.
           SET R TO SELF.
       END METHOD MK.
       END OBJECT.
       END CLASS PB451N8C.
