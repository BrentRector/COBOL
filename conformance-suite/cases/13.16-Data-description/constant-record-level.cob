*>--- conformance-case
*> id: constant-record-level
*> rules: [SR-13.16.3-6]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.16.3 SR6 - the CONSTANT RECORD clause may be specified only in data description
*> entries whose level-number is 1 (COBOLNET1549).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGKC08.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G.
   05 CFG CONSTANT RECORD.
      10 CFG-TAG PIC X(4) VALUE "COBL".
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
