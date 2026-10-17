*>--- conformance-case
*> id: pb575-cond-name-bit-group-alnum-value
*> rules: [SR-13.18.63.3-24]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.63.3 SR24 -> SR10: a boolean subject's VALUE literals shall be boolean literals or ZERO; a plain
*> alphanumeric literal does not conform. 13.18.29.4 GR1 b) gives a GROUP-USAGE BIT group "class and category
*> boolean", so the CLASS half of SR10 binds a bit-group conditional variable exactly as it binds the elementary
*> twin (the SIZE half does not reach a condition-name subject at all -- 13.18.63.3 SR4/SR5/SR10 name "an
*> elementary item" and a "group item" as the VALUE-clause bearer, kb/Work PB598). The screen used to read the
*> raw PICTURE, which a group does not have, so this compiled AND RAN (kb/Work PB575 + PB728).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB575A.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 BG GROUP-USAGE BIT.
   88 C-ALNUM VALUE "AB".
   05 E1 PIC 1(4) USAGE BIT.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
