*>--- conformance-case
*> id: pb451-classname-sender-active-class-receiver
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 14: "If the data item referenced by identifier-3 is
      *> described with an ACTIVE-CLASS phrase, the data item referenced by identifier-4 shall
      *> be one of the following: a) an object reference described with the ACTIVE-CLASS
      *> phrase …; b) the predefined object SELF …; c) the predefined object reference NULL."
      *> A class NAME is in none of the three, and neither SR11 nor SR13 reaches this
      *> statement — each names a DIFFERENT receiver description in its own precondition.
      *> That is why the diagnostic has to read the governing rule off the RECEIVER: this same
      *> statement shape is legal under SR13 with an object-class-name receiver.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB451N4.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB451N4C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB451N4C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB451N4C "NEW" RETURNING O.
           INVOKE O "MK".
           STOP RUN.
       END PROGRAM PB451N4.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB451N4C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
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
       01 R USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION.
       MAIN.
           SET R TO PB451N4C.
       END METHOD MK.
       END OBJECT.
       END CLASS PB451N4C.
