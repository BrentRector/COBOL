*>--- conformance-case
*> id: constant-record-store
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.15.3 SR2 - neither a CONSTANT RECORD's subject nor any subordinate shall be
*> specified as a receiving data item; the structured constant's content cannot be modified
*> (COBOLNET1548).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGKC04.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 CFG CONSTANT RECORD.
   05 CFG-TAG PIC X(4) VALUE "COBL".
PROCEDURE DIVISION.
MAIN.
    MOVE "XXXX" TO CFG-TAG.
    STOP RUN.
