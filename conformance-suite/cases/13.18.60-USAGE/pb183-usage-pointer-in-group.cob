*>--- conformance-case
*> id: pb183-usage-pointer-in-group
*> rules: [SR-13.18.60.3-14]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.60.3 SR14 (kb/Work PB183) - THE HEADLINE CASE. "A USAGE clause with the MESSAGE-TAG,
*> OBJECT REFERENCE, POINTER, FUNCTION-POINTER, or PROGRAM-POINTER phrase may be specified only for
*> an elementary data item at level 1 or an elementary data item subordinate to a type declaration
*> that includes the STRONG phrase." P is at level 05 under an ORDINARY group, so neither arm admits
*> it. Verified against the PRINTED page (folio 505) before the screen was written.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB183A.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G.
   05 P USAGE POINTER.
   05 F PIC X(4).
PROCEDURE DIVISION.
MAIN.
    SET P TO NULL.
    STOP RUN.
