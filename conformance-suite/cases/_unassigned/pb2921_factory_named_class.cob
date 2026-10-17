*>--- conformance-case
*> id: pb2921_factory_named_class
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2921 - a class-name ending in --FACTORY is a legal
      *> word, and its class is not the factory object of another class.
      *> ISO 8.3.2.1: "The hyphen or underscore shall not appear as the
      *>   first or last character in such words" -> OK 8.3.2.1, so
      *>   X--FACTORY is a user-defined word naming a class beside the
      *>   class X. The compiler named X's factory class X__FACTORY, the
      *>   C# identifier of X--FACTORY, and refused the second class
      *>   COBOLNET0820.
      *> Expected values: 9.3.6 "If an invocation specifies the object
      *>   using a class name, the factory object of the specified class
      *>   is used" -> each INVOKE reaches its own class's factory, and
      *>   8.6.4 gives each factory object "one copy of each static item"
      *>   of its own: X counts 1 2, X--FACTORY counts 1 (never 3), and
      *>   each HELLO prints its own class.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2921P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS X
           CLASS X--FACTORY.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE X "HELLO"
           INVOKE X "HELLO"
           INVOKE X--FACTORY "HELLO"
           STOP RUN.
       END PROGRAM PB2921P.

       IDENTIFICATION DIVISION.
       CLASS-ID. X.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. HELLO.
       PROCEDURE DIVISION.
           ADD 1 TO CNT
           DISPLAY "X " CNT.
       END METHOD HELLO.
       END FACTORY.
       END CLASS X.

       IDENTIFICATION DIVISION.
       CLASS-ID. X--FACTORY.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. HELLO.
       PROCEDURE DIVISION.
           ADD 1 TO CNT
           DISPLAY "X--FACTORY " CNT.
       END METHOD HELLO.
       END FACTORY.
       END CLASS X--FACTORY.
