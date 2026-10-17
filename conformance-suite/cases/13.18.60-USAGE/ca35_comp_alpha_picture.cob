*>--- conformance-case
*> id: ca35_comp_alpha_picture
*> rules: [SR-13.18.60.3-3]
*> source-format: free
*> portable: unverified
*>---
*> CA35 (CONFORMANCE-FIX-QUEUE): USAGE BINARY/COMPUTATIONAL/PACKED-DECIMAL shall be specified only with a picture
*> that describes a NUMERIC item (ISO 13.18.60.3 SR3). PIC XX is alphanumeric, so USAGE COMP is illegal ->
*> rejected (COBOLNET0881), mirroring the enforced USAGE BIT SR5 case. Pre-fix it silently bound as category
*> Alphanumeric with the COMP usage dropped. Edition-invariant (85/2002/2014/2023).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGCA35A.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC XX COMP.
PROCEDURE DIVISION.
    DISPLAY "X".
    STOP RUN.
