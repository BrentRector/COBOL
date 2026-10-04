      *> reject-at: 2002 2014 2023
      *> kb/Work PB1051 -- ISO 14.9.23.3 SR15 (cite.py --check 14.9.23.3 "If identifier-5 or its
      *> corresponding formal parameter is specified with the BY VALUE phrase, identifier-5 shall be of
      *> class message-tag, numeric, object or pointer" -> OK 14.9.23.3 15)): X is alphanumeric.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1051N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CBVN1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CBVN1.
       01 N PIC S9(4) COMP-5 VALUE 42.
       01 X PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CBVN1 "NEW" RETURNING O.
           INVOKE O "M" USING BY VALUE X.
           STOP RUN.
       END PROGRAM PB1051N1.

       IDENTIFICATION DIVISION.
       CLASS-ID. CBVN1 INHERITS FROM BASE.
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
       END CLASS CBVN1.
