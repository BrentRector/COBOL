      *> reject-at: 2002 2014 2023
      *> kb/Work PB1273 — ISO/IEC 1989:2023 §13.18.42.3 SR2: "The PROPERTY clause shall not be specified for data
      *> items subject to an OCCURS clause." E is subject to T's OCCURS by subordination. Before the fix only the
      *> subject's OWN OCCURS was asked, so this reached the C# backend and failed there (CS0103).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1273NOM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1273NOC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE PB1273NOC.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1273NOC "NEW" RETURNING A.
           CONTINUE.
           STOP RUN.
       END PROGRAM PB1273NOM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1273NOC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T OCCURS 3.
             10 E PIC 9 PROPERTY.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
       MAIN.
           CONTINUE.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB1273NOC.
