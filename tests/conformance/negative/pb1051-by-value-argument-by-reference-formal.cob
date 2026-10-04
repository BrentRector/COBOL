      *> reject-at: 2002 2014 2023
      *> kb/Work PB1051 -- ISO 14.9.23.3 SR5 b) (cite.py --check 14.9.23.3 "If a BY VALUE phrase is
      *> specified for an argument, a BY VALUE phrase shall be specified or implied for the corresponding
      *> formal parameter" -> OK 14.9.23.3 5) b)): the formal is BY REFERENCE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1051N4.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CBVN4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CBVN4.
       01 N PIC S9(4) COMP-5 VALUE 42.
       01 X PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CBVN4 "NEW" RETURNING O.
           INVOKE O "M" USING BY VALUE N.
           STOP RUN.
       END PROGRAM PB1051N4.

       IDENTIFICATION DIVISION.
       CLASS-ID. CBVN4 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC S9(4) COMP-5.
       PROCEDURE DIVISION USING LN.
           CONTINUE.
       END METHOD M.
       END OBJECT.
       END CLASS CBVN4.
