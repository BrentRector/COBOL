*>--- conformance-case
*> id: numedit_digits_over18_at85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> CA33 (CONFORMANCE-FIX-QUEUE): ISO 13.18.40.3 SR14 measures the 1-31 (18 pre-2002) capacity cap against DIGIT
*> POSITIONS (9/Z/* + P + floating), not just the '9' count. Z(11)9(8) is 19 digit positions -> exceeds the COBOL-85
*> 18-digit cap (COBOLNET0802). Accepted at 2002+ (19 <= 31). Pre-fix it slipped past (Digits counted only the 8 nines).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGCA33A.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC Z(11)9(8).
PROCEDURE DIVISION.
    DISPLAY "X".
    STOP RUN.
