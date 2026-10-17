*>--- conformance-case
*> id: external_file_status_consistency
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XFSCPA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN "fdata" FILE STATUS IS EXT-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F IS EXTERNAL.
       01 REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 EXT-ST IS EXTERNAL PIC XX.
       PROCEDURE DIVISION.
       MAIN-A.
           STOP RUN.
       END PROGRAM XFSCPA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XFSCPB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN "fdata" FILE STATUS IS EXT-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F IS EXTERNAL.
       01 REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 EXT-ST IS EXTERNAL PIC XX.
       PROCEDURE DIVISION.
       MAIN-B.
           GOBACK.
       END PROGRAM XFSCPB.
