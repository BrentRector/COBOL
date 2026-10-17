*>--- conformance-case
*> id: program-pointer-value-clause
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.63.3 SR9 - "The VALUE clause shall not be specified if a USAGE clause with a phrase
*> of FUNCTION-POINTER, MESSAGE-TAG, OBJECT-REFERENCE, or PROGRAM-POINTER is also specified." (COBOLNET2168)
*> kb/Work PB557 moved this off COBOLNET0881's general usage-x-clause band onto SR9's own code and restated the
*> message over the whole set: the screen used to cover two of the rule's four usages, so an OBJECT REFERENCE
*> entry with a VALUE literal ran clean and one with VALUE NULL failed the backend compilation. The rule, the
*> rejection and the edition band are unchanged; pb557-value-on-object-reference and pb557-value-null-on-pointer
*> cover the arms this one never reached.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPP04.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 PP USAGE PROGRAM-POINTER VALUE "X".
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
