*>--- conformance-case
*> id: pb451-active-class-sender-unrelated-class-receiver
*> rules: [SR-14.9.39.3-12]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 12 b)2.: with identifier-3 described with an
      *> object-class-name and identifier-4 "an object reference described with an ACTIVE-CLASS
      *> phrase", "the class containing the data item referenced by identifier-4 shall be the
      *> same class or a subclass of the class specified in the description of the data item
      *> referenced by identifier-3."  §13.18.60.4 GR22 e) is why: an ACTIVE-CLASS reference
      *> holds an object of the class that INVOKED the method, which is the containing class
      *> or a subclass of it — so if the containing class is not the receiver's class or a
      *> subclass, no invocation can make the contents conform.
      *> b)1. and b)3., its siblings, are pinned by conformance:negative/
      *> pb389-only-receiver-active-class-sender and …/pb389-active-class-factory-presence;
      *> the CONFORMING direction of b)2. is the `SET B3 TO A` line of
      *> conformance:cases/9.3.6-Method-invocation/pb389_object_reference_descriptor.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB451NA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB451NAC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB451NAC.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB451NAC "NEW" RETURNING O.
           INVOKE O "MK".
           STOP RUN.
       END PROGRAM PB451NA.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451NAU.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB451NAU.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451NAC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB451NAU.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE ACTIVE-CLASS.
       01 R USAGE OBJECT REFERENCE PB451NAU.
       PROCEDURE DIVISION.
       MAIN.
           SET A TO SELF.
           SET R TO A.
       END METHOD MK.
       END OBJECT.
       END CLASS PB451NAC.
