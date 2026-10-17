*>--- conformance-case
*> id: oo_property_factory_ref
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOPRF3.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CACC9
           PROPERTY FCNT.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 42 TO FCNT OF CACC9.
           DISPLAY FCNT OF CACC9.
           STOP RUN.
       END PROGRAM OOPRF3.

       IDENTIFICATION DIVISION.
       CLASS-ID. CACC9.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FCNT PIC 9(4) VALUE 1 PROPERTY.
       PROCEDURE DIVISION.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS CACC9.
