      *> reject-at: 2002 2014 2023
      *> kb/Work PB1273 — ISO/IEC 1989:2023 §13.18.42.3 SR1: "The PROPERTY clause may be specified only in the
      *> working-storage section of a factory definition or an instance definition." N is in a METHOD's
      *> LOCAL-STORAGE. Before the fix the clause was silently inert there (and in a program's working storage).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1273NLM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1273NLC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE PB1273NLC.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1273NLC "NEW" RETURNING A.
           CONTINUE.
           STOP RUN.
       END PROGRAM PB1273NLM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1273NLC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 N PIC 9 VALUE 3 PROPERTY.
       PROCEDURE DIVISION.
       MAIN.
           CONTINUE.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB1273NLC.
