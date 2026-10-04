      *> reject-at: 2002 2014 2023
      *> kb/Work PB1051 -- ISO 14.9.23.3 SR15 through the INLINE form (8.4.3.4.3 SR3 imports the syntax rules of
      *> 14.9.23.3; cite.py --check 14.9.23.3 "If identifier-5 or its corresponding formal parameter is specified
      *> with the BY VALUE phrase, identifier-5 shall be of class message-tag, numeric, object or pointer" -> OK
      *> 14.9.23.3 15)). The inline argument has no BY phrase; the FORMAL is BY VALUE, so (GR6 b)) the argument is
      *> BY VALUE and X, an alphanumeric item, is of a class that cannot be passed by value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1051N8.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CBVN8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CBVN8.
       01 X PIC X(4) VALUE "ABCD".
       01 TOT PIC S9(6) COMP-5.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CBVN8 "NEW" RETURNING O.
           MOVE O :: "TWICE" (X) TO TOT.
           STOP RUN.
       END PROGRAM PB1051N8.

       IDENTIFICATION DIVISION.
       CLASS-ID. CBVN8 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TWICE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC S9(4) COMP-5.
       01 R PIC S9(6) COMP-5.
       PROCEDURE DIVISION USING BY VALUE A RETURNING R.
           COMPUTE R = A * 2.
       END METHOD TWICE.
       END OBJECT.
       END CLASS CBVN8.
