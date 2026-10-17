*>--- conformance-case
*> id: external-relative-key-externality-at-2023
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 14.8.4.2 conjunct 1 (Annex E.2 item 9): at >=2023 the RELATIVE KEY data item of an
*> external relative file connector shall ITSELF be an external data item. Here a SINGLE
*> program declares FD F IS EXTERNAL ORGANIZATION RELATIVE with RELATIVE KEY IS WS-RK, where
*> WS-RK is a plain (non-external) item. Below 2023 the requirement did not exist.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XRKNE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN "fdata" ORGANIZATION RELATIVE
               ACCESS RANDOM RELATIVE KEY IS WS-RK.
       DATA DIVISION.
       FILE SECTION.
       FD F IS EXTERNAL.
       01 REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 WS-RK PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
