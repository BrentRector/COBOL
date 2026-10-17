*>--- conformance-case
*> id: external-constant-record-no-type-at-2023
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> VCR 16 (ISO 13.16.3 SR13 para 2; Annex E.2 item 10): at >=2023 an EXTERNAL CONSTANT
*> RECORD requires a TYPE clause naming a strongly typed definition. A bare external
*> constant record (no TYPE) is rejected COBOLNET1549. Below 2023 it is the legacy
*> accepted form (the 2014 positive corpus pins that half).
IDENTIFICATION DIVISION.
PROGRAM-ID. EXTCRNOTYPE.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 R IS EXTERNAL CONSTANT RECORD.
   05 A PIC X(4) VALUE "ABCD".
PROCEDURE DIVISION.
MAIN.
    DISPLAY A OF R.
    STOP RUN.
