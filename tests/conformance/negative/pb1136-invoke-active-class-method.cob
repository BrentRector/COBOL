      *> reject-at: 2002 2014 2023
      *> ISO §14.9.23.3 SR4 d): "If identifier-1 references an object
      *> reference described with the ACTIVE-CLASS phrase and without the
      *> FACTORY phrase, literal-1 shall be the name of a method contained in
      *> the instance interface of the class containing the INVOKE
      *> statement."  PB1136C4 has no instance method NOPE, so the INVOKE
      *> through the ACTIVE-CLASS reference R is refused under SR4 d) - the
      *> diagnostic used to print SR4 b) (the object-class-name arm) for
      *> every non-factory typed receiver.  kb/Work PB1136.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1136N4.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
       END PROGRAM PB1136N4.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1136C4 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WORK.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 R USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION.
       MAIN.
           SET R TO SELF.
           INVOKE R "NOPE".
       END METHOD WORK.
       END OBJECT.
       END CLASS PB1136C4.
