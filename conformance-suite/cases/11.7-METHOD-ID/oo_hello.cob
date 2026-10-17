*>--- conformance-case
*> id: oo_hello
*> rules: [GR-11.7.4-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §11.2/§11.7/§14.9.23 (OO COBOL, COBOL-2002) — the minimal object-oriented program:
      *> a driver PROGRAM creates an instance of a CLASS with the built-in NEW factory (§16.2.1), stores it in
      *> a USAGE OBJECT REFERENCE item, and INVOKEs an instance method that DISPLAYs the object's OBJECT data.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OODEMO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS GREETER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G USAGE OBJECT REFERENCE GREETER.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE GREETER "NEW" RETURNING G.
           INVOKE G "SAYHELLO".
           STOP RUN.
       END PROGRAM OODEMO.

       IDENTIFICATION DIVISION.
       CLASS-ID. GREETER INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MSG PIC X(13) VALUE "HELLO, WORLD!".
       PROCEDURE DIVISION.
       METHOD-ID. SAYHELLO.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY MSG.
       END METHOD SAYHELLO.
       END OBJECT.
       END CLASS GREETER.
