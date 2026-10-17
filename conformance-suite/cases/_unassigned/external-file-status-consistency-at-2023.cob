*>--- conformance-case
*> id: external-file-status-consistency-at-2023
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> VCR 18 (ISO 12.4.5.3 GR1(i); 14.8.4.2; Annex E.2 item 12): at >=2023 all corresponding
*> SELECTs of an EXTERNAL file shall specify FILE STATUS naming the same corresponding
*> external data item. Here program B omits FILE STATUS on the shared external file F.
*> Below 2023 the requirement did not exist (the 2014 continuity golden pins that half).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XFSCA.
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
       END PROGRAM XFSCA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XFSCB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN "fdata".
       DATA DIVISION.
       FILE SECTION.
       FD F IS EXTERNAL.
       01 REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-B.
           GOBACK.
       END PROGRAM XFSCB.
