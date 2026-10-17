*>--- conformance-case
*> id: pb728-cond-name-bit-group-through
*> rules: [SR-13.18.63.3-29]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.63.3 SR29: "If the category of the subject of the entry is boolean, the THROUGH phrase shall not
*> be specified." 13.18.29.4 GR1 b) makes the antecedent real for a GROUP: "a bit group is treated as though it
*> were an elementary data item of usage bit and class and category boolean described with PICTURE 1(m)". So a
*> GROUP-USAGE BIT conditional variable is category boolean and SR29 bars THROUGH on it, exactly as it does on
*> the elementary twin. The screen used to read the raw PICTURE, which a group does not have, so only the
*> elementary arm was enforced (kb/Work PB575 + PB728).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB728A.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 GB GROUP-USAGE BIT.
   88 GB-RANGE VALUE B"000" THRU B"111".
   05 GB-A PIC 1(3) USAGE BIT.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
