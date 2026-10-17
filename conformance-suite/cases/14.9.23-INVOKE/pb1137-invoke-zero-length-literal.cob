      *>--- conformance-case
      *> id: pb1137-invoke-zero-length-literal
      *> rules: [SR-14.9.23.3-17]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1137 - ISO 14.9.23.3 SR17: "Literal-2 shall not be a
      *> zero-length literal."
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1137N4.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1137N4K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1137N4K.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1137N4K "NEW" RETURNING O.
           INVOKE O "TAKE" USING BY CONTENT "".
           STOP RUN.
       END PROGRAM PB1137N4.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1137N4K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA PIC X(5).
       PROCEDURE DIVISION USING LA.
           CONTINUE.
       END METHOD TAKE.
       END OBJECT.
       END CLASS PB1137N4K.
