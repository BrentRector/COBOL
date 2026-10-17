*>--- conformance-case
*> id: pb183-usage-objref-in-group
*> rules: [SR-13.18.60.3-14]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.60.3 SR14 - the OBJECT REFERENCE category. One fixture per rejecting category: a single
*> case cannot distinguish which of SR14's phrases the screen's class predicate actually covers, and
*> a class predicate that silently lost one member would still pass every other fixture here.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB183B.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G.
   05 O USAGE OBJECT REFERENCE.
   05 F PIC X(4).
PROCEDURE DIVISION.
MAIN.
    SET O TO NULL.
    STOP RUN.
