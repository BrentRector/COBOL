*>--- conformance-case
*> id: pb761-national-through-alnum-literals
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB761 - the UNDER-REJECTION that discharging the COBOLNET0899
*> national-THROUGH stage would otherwise have opened. While that stage refused
*> EVERY national THROUGH range, no such range ever reached the VALUE category
*> funnel, so removing the stage alone would have let alphanumeric literals seed
*> a national conditional variable in silence.
*> ISO 13.18.63.3 SR5: the VALUE of a category-national subject shall be a
*> national literal or a figurative constant, and SR24 routes a Format-3
*> (condition-name) entry to it. A THROUGH range bound is a VALUE operand like
*> any other, so both bounds are screened.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB761A.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 EN PIC N(3).
   88 EN-BAD-RANGE VALUE "AAA" THRU "CCC".
PROCEDURE DIVISION.
MAIN.
    IF EN-BAD-RANGE DISPLAY "Y" ELSE DISPLAY "N" END-IF
    STOP RUN.
