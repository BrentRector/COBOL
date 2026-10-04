      *> reject-at: 2002 2014 2023
      *> ISO §14.9.23.3 SR7: "Identifier-2 may be specified only when
      *> identifier-1 is a universal object reference."  O is typed
      *> (OBJECT REFERENCE PB1136C6), so the method name held in MA is
      *> refused - a typed receiver binds its method statically from
      *> literal-1 (SR4).  kb/Work PB1136.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1136N6.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1136C6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O  USAGE OBJECT REFERENCE PB1136C6.
       01 MA PIC X(5) VALUE "GREET".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE O MA.
           STOP RUN.
       END PROGRAM PB1136N6.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1136C6 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GREET.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "HI".
       END METHOD GREET.
       END OBJECT.
       END CLASS PB1136C6.
