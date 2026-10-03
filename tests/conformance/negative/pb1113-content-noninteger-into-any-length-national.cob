      *> reject-at: 2002 2014 2023
      *> kb/Work PB1113 -- ISO 14.8.2.3.3 2) c): "If the formal parameter
      *> is described with the ANY LENGTH clause, its length is considered
      *> to match the length of the corresponding argument" -- a statement
      *> about LENGTH only, so the category pair is still 2) d)'s MOVE
      *> question, and 14.9.25.3 Table 16 says numeric-noninteger ->
      *> national is "No". The BY CONTENT PIC 9V9 argument into the
      *> PIC N ANY LENGTH method formal is COBOLNET0828. (Before the fix an
      *> ANY LENGTH formal skipped the question and the method printed the
      *> digits.)
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1113L INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS BASE CLASS PB1113L.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. MA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC N ANY LENGTH.
       PROCEDURE DIVISION USING L.
           DISPLAY "IN-MA"
           GOBACK.
       END METHOD MA.
       END OBJECT.
       END CLASS PB1113L.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1113M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS PB1113L.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1113L.
       01 NI PIC 9V9 VALUE 1.5.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1113L "NEW" RETURNING O
           INVOKE O "MA" USING BY CONTENT NI
           STOP RUN.
       END PROGRAM PB1113M.
