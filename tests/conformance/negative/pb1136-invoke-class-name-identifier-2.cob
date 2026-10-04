      *> reject-at: 2002 2014 2023
      *> ISO §14.9.23.3 SR3: "If object-class-name-1 is specified, literal-1
      *> shall be specified."  (SR7: "Identifier-2 may be specified only when
      *> identifier-1 is a universal object reference.")  PB1136C2 is a
      *> class-name, so the method name held in MA is refused under SR3 -
      *> the compile used to resolve PB1136C2 as a data item first and print
      *> the resolver's false "'PB1136C2' is not defined".  kb/Work PB1136.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1136N2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1136C2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MA PIC X(5) VALUE "GREET".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1136C2 MA.
           STOP RUN.
       END PROGRAM PB1136N2.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1136C2 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. GREET.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "HI".
       END METHOD GREET.
       END FACTORY.
       END CLASS PB1136C2.
