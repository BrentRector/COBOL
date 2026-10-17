*>--- conformance-case
*> id: pb183-usage-progptr-in-group
*> rules: [SR-13.18.60.3-14]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.60.3 SR14 - the PROGRAM-POINTER category, the third of the three the model can express
*> today. (FUNCTION-POINTER is staged loud at ParseUsage - the P13 prototype band - and MESSAGE-TAG
*> has no Usage member yet; a unit drift test holds both forward obligations open.)
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB183C.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G.
   05 PP USAGE PROGRAM-POINTER.
   05 F PIC X(4).
PROCEDURE DIVISION.
MAIN.
    SET PP TO NULL.
    STOP RUN.
