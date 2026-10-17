*>--- conformance-case
*> id: pb575-cond-name-national-group-alnum-value
*> rules: [SR-13.18.63.3-24]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.63.3 SR5: a category-national subject's VALUE literals shall be national literals or figurative
*> constants; a plain alphanumeric literal does not conform. 13.18.29.4 GR2 b) gives a GROUP-USAGE NATIONAL
*> group "class and category national", so SR5's class rule binds a national-group conditional variable exactly
*> as it binds the elementary twin. The screen used to read the raw PICTURE, which a group does not have, so
*> this compiled AND RAN (kb/Work PB575 + PB728).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB575B.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 NG GROUP-USAGE NATIONAL.
   88 N-ALNUM VALUE "AB".
   05 E2 PIC N(3).
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
