      *> reject-at: 2002 2014 2023
      *> kb/Work PB1051 -- ISO 14.9.23.3 SR5 a) (cite.py --check 14.9.23.3 "If a BY CONTENT or BY
      *> REFERENCE phrase is specified for an argument, a BY REFERENCE phrase shall be specified or
      *> implied for the corresponding formal parameter" -> OK 14.9.23.3 5) a)): the formal is BY VALUE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1051N3.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CBVN3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CBVN3.
       01 N PIC S9(4) COMP-5 VALUE 42.
       01 X PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CBVN3 "NEW" RETURNING O.
           INVOKE O "M" USING BY REFERENCE N.
           STOP RUN.
       END PROGRAM PB1051N3.

       IDENTIFICATION DIVISION.
       CLASS-ID. CBVN3 INHERITS FROM BASE.
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
       PROCEDURE DIVISION USING BY VALUE LN.
           CONTINUE.
       END METHOD M.
       END OBJECT.
       END CLASS CBVN3.
