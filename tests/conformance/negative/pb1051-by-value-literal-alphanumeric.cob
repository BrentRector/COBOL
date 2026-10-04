      *> reject-at: 2002 2014 2023
      *> kb/Work PB1051 -- ISO 14.9.23.3 SR16 (cite.py --check 14.9.23.3 "If literal-2 or its
      *> corresponding formal parameter is specified with the BY VALUE phrase, literal-2 shall be a
      *> numeric literal" -> OK 14.9.23.3 16)): "AB" is not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1051N2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CBVN2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CBVN2.
       01 N PIC S9(4) COMP-5 VALUE 42.
       01 X PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CBVN2 "NEW" RETURNING O.
           INVOKE O "M" USING BY VALUE "AB".
           STOP RUN.
       END PROGRAM PB1051N2.

       IDENTIFICATION DIVISION.
       CLASS-ID. CBVN2 INHERITS FROM BASE.
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
       END CLASS CBVN2.
