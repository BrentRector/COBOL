*>--- conformance-case
*> id: external_relative_key_consistency
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XRKCPA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELFILE ASSIGN TO "reldata"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RELKEY.
       DATA DIVISION.
       FILE SECTION.
       FD RELFILE IS EXTERNAL.
       01 RELREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RELKEY IS EXTERNAL PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-A.
           STOP RUN.
       END PROGRAM XRKCPA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XRKCPB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELFILE ASSIGN TO "reldata"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RELKEY.
       DATA DIVISION.
       FILE SECTION.
       FD RELFILE IS EXTERNAL.
       01 RELREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RELKEY IS EXTERNAL PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-B.
           GOBACK.
       END PROGRAM XRKCPB.
