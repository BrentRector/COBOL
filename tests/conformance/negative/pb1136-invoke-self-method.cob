      *> reject-at: 2002 2014 2023
      *> ISO §14.9.23.3 SR4 g): "If identifier-1 references the predefined
      *> object reference SELF and the method containing the INVOKE statement
      *> is an instance method, literal-1 shall be the name of a method
      *> contained in the instance interface of the class containing the
      *> INVOKE statement."  PB1136C5 has no instance method NOPE, so the
      *> diagnostic names SR4 g) - it used to print the range "SR4f-SR4i"
      *> for every SELF/SUPER arm.  kb/Work PB1136.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1136N5.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
       END PROGRAM PB1136N5.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1136C5 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WORK.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE SELF "NOPE".
       END METHOD WORK.
       END OBJECT.
       END CLASS PB1136C5.
