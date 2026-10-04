      *> reject-at: 2002 2014 2023
      *> kb/Work PB1051 -- ISO 14.2.2 SR2 (cite.py --check 14.2.2 "Each data-name-1 specified in a BY
      *> VALUE phrase shall be defined as a data item of class numeric, message-tag, object, or pointer"
      *> -> OK 14.2.2 2)): LX is alphanumeric.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1051N5.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CBVN5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CBVN5.
       01 N PIC S9(4) COMP-5 VALUE 42.
       01 X PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CBVN5 "NEW" RETURNING O.
           INVOKE O "M" USING BY VALUE N.
           STOP RUN.
       END PROGRAM PB1051N5.

       IDENTIFICATION DIVISION.
       CLASS-ID. CBVN5 INHERITS FROM BASE.
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
       01 LX PIC X(4).
       PROCEDURE DIVISION USING BY VALUE LX.
           CONTINUE.
       END METHOD M.
       END OBJECT.
       END CLASS CBVN5.
