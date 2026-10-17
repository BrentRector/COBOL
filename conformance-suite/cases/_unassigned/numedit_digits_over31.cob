*>--- conformance-case
*> id: numedit_digits_over31
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> CA33 (CONFORMANCE-FIX-QUEUE): an all-suppression numeric-edited picture Z(35) has 35 DIGIT POSITIONS (ISO
*> 13.18.40.4 GR14 - Z/* are digit-bearing positions) -> exceeds the 31-digit cap (COBOLNET0801) at every edition.
*> Pre-fix its Digits was 0 (no '9'), so the > 0 guard skipped the cap check entirely and it was accepted.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGCA33B.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 C PIC Z(35).
PROCEDURE DIVISION.
    DISPLAY "X".
    STOP RUN.
