*>--- conformance-case
*> id: external-relative-key-consistency-at-2023
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> VCR 31 (ISO 12.4.5.3 GR1(h); 14.8.4.2; Annex E.2 item 24): at >=2023 all corresponding
*> SELECTs of an EXTERNAL relative file shall specify RELATIVE KEY naming the same
*> corresponding external data item. Here program B's RELATIVE KEY (RELKEY) is a LOCAL
*> (non-external) item. Below 2023 the requirement did not exist.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XRKCA.
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
       END PROGRAM XRKCA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XRKCB.
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
       01 RELKEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-B.
           GOBACK.
       END PROGRAM XRKCB.
