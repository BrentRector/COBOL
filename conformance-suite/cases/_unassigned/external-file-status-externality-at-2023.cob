*>--- conformance-case
*> id: external-file-status-externality-at-2023
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 14.8.4.2 conjunct 1 (Annex E.2 item 9): at >=2023 the FILE STATUS data item of an
*> external file connector shall ITSELF be an external data item. Here a SINGLE program
*> declares FD F IS EXTERNAL with FILE STATUS IS WS-ST, where WS-ST is a plain (non-external)
*> WORKING-STORAGE item. This exercises the lone-describer externality face (there is no
*> second corresponding SELECT to reconcile). Below 2023 the requirement did not exist.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XFSNE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN "fdata" FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F IS EXTERNAL.
       01 REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
