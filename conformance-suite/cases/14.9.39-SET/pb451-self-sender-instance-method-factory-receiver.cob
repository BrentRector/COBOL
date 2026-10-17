*>--- conformance-case
*> id: pb451-self-sender-instance-method-factory-receiver
*> rules: [SR-14.9.39.3-12]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 12 c)4.: "if the data item referenced by identifier-3 is
      *> described with a FACTORY phrase, the method containing the SET statement shall be
      *> defined in the factory definition of its containing class."  The mirror of c)3. and
      *> the reason both are ONE predicate: inside an INSTANCE method SELF is an instance
      *> object, and a FACTORY-described receiver holds the class's factory object
      *> (§13.18.60.4 GR22 d)1.a.) — two different objects of two different method interfaces
      *> (§9.3.6).  Until the FACTORY axis could be DECLARED (kb/Work PB389) this rule had no
      *> writable subject at all.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB451N6.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB451N6C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB451N6C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB451N6C "NEW" RETURNING O.
           INVOKE O "MK".
           STOP RUN.
       END PROGRAM PB451N6.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451N6C INHERITS FROM BASE.
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
       01 R USAGE OBJECT REFERENCE FACTORY OF PB451N6C.
       PROCEDURE DIVISION.
       MAIN.
           SET R TO SELF.
       END METHOD MK.
       END OBJECT.
       END CLASS PB451N6C.
