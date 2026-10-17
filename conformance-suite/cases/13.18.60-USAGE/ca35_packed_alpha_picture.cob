*>--- conformance-case
*> id: ca35_packed_alpha_picture
*> rules: [SR-13.18.60.3-3]
*> source-format: free
*> portable: unverified
*>---
*> CA35 (CONFORMANCE-FIX-QUEUE): the SR3 guard covers USAGE PACKED-DECIMAL (= COMP-3) too. PIC A(4) is alphabetic,
*> so USAGE PACKED-DECIMAL is illegal -> rejected (COBOLNET0881, ISO 13.18.60.3 SR3). Edition-invariant.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGCA35B.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 B PIC A(4) USAGE PACKED-DECIMAL.
PROCEDURE DIVISION.
    DISPLAY "X".
    STOP RUN.
