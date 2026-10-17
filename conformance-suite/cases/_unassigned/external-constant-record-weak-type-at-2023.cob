*>--- conformance-case
*> id: external-constant-record-weak-type-at-2023
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> VCR 16 STRENGTH half (ISO §13.16.3 SR13 ¶2; Annex E.2 item 10; review finding C9):
*> at >=2023 an EXTERNAL CONSTANT RECORD requires a TYPE naming a STRONGLY typed
*> definition. WK here is a WEAK typedef — presence alone passed the earlier gate;
*> the strength half must reject (COBOLNET1549). Below 2023 the requirement did not
*> exist (the 2014 golden external_constant_record_weak_type pins the continuity).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XCRWEAK.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WK TYPEDEF.
          05 WK-A PIC X(4) VALUE "WXYZ".
       01 CR IS EXTERNAL CONSTANT RECORD TYPE WK.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X"
           STOP RUN.
