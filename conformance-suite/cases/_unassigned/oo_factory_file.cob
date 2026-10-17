*>--- conformance-case
*> id: oo_factory_file
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOFF1.
      *> M2-OO-1i inc 3: a FACTORY-paragraph FILE-CONTROL + FILE SECTION.
      *> The factory (class singleton, ISO §9.3.14.2) owns one file connector,
      *> registered once in the singleton ctor; a factory method round-trips it.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS FFCLS.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE FFCLS "DOIT".
           STOP RUN.
       END PROGRAM OOFF1.

       IDENTIFICATION DIVISION.
       CLASS-ID. FFCLS.
       IDENTIFICATION DIVISION.
       FACTORY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FF ASSIGN TO "oo-fac.dat".
       DATA DIVISION.
       FILE SECTION.
       FD FF.
       01 FF-REC PIC X(12).
       PROCEDURE DIVISION.
       METHOD-ID. DOIT.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FF.
           MOVE "FACTORY DATA" TO FF-REC.
           WRITE FF-REC.
           CLOSE FF.
           OPEN INPUT FF.
           READ FF AT END DISPLAY "EOF".
           DISPLAY "FGOT=" FF-REC.
           CLOSE FF.
       END METHOD DOIT.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS FFCLS.
