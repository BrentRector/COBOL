*>--- conformance-case
*> id: oo_universal_relation
*> rules: [GR-8.4.3.7.4-1, GR-8.8.4.2.1-10]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OOUV4.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CUV4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       01 G USAGE OBJECT REFERENCE CUV4.
       PROCEDURE DIVISION.
       MAIN.
           IF U = NULL DISPLAY "U-IS-NULL" END-IF.
           INVOKE CUV4 "NEW" RETURNING G.
           SET U TO G.
           IF U NOT = NULL DISPLAY "U-NOT-NULL" END-IF.
           IF U = G DISPLAY "SAME-OBJECT" END-IF.
           INVOKE CUV4 "NEW" RETURNING G.
           IF U NOT = G DISPLAY "DIFFERENT-OBJECT" END-IF.
           STOP RUN.
       END PROGRAM OOUV4.

       IDENTIFICATION DIVISION.
       CLASS-ID. CUV4 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS CUV4.
