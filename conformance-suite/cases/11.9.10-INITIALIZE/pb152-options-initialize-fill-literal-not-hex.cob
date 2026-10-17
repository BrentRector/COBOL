*>--- conformance-case
*> id: pb152-options-initialize-fill-literal-not-hex
*> rules: [SR-11.9.10.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 11.9.10.3 SR1 (kb/Work PB152): "Literal-1 shall specify a one-byte hexadecimal-alphanumeric
*> literal."
*>
*> ⛔ "Hexadecimal-alphanumeric literal" is a DEFINED TERM, not loose wording for "a short quoted
*> literal". 8.3.3.2.2 gives the alphanumeric literal exactly TWO general formats - format 1 ("..."
*> or '...') and format 2 (X"..." or X'...') - and "hexadecimal-alphanumeric" names FORMAT 2. So a
*> one-character format-1 literal is NOT admitted however short it is; X"5A" is the conforming
*> spelling of the same byte. This is the one place a natural reading of SR1 diverges from what it
*> says, and it is why the rule needed reading rather than paraphrasing.
*>
*> Measured before the screen: the decoder took raw[0] for ANY shape, so this compiled and silently
*> filled with 'Z'.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB152A.
OPTIONS.
    INITIALIZE ALL TO "Z".
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC X(4).
PROCEDURE DIVISION.
MAIN.
    DISPLAY A.
    STOP RUN.
