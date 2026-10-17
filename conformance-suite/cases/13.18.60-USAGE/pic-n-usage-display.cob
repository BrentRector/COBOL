*>--- conformance-case
*> id: pic-n-usage-display
*> rules: [SR-13.18.60.3-20]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.60.4 SR20: PIC N admits only USAGE NATIONAL (SR13a implies it when no USAGE clause).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGNB03.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-N PIC N(4) USAGE DISPLAY.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
