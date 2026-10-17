*>--- conformance-case
*> id: pb152-options-initialize-fill-literal-two-bytes
*> rules: [SR-11.9.10.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 11.9.10.3 SR1's OTHER half (kb/Work PB152): the literal is the right FORMAT here - X"..." is
*> 8.3.3.2.2's format 2 - but it is TWO bytes, and SR1 says "a ONE-BYTE hexadecimal-alphanumeric
*> literal". One byte is exactly two hexadecimal digits (8.3.3.2.3 SR5: "Hex-character-sequence-1
*> shall be composed of hexadecimal digits").
*>
*> Written as its own fixture because a single fixture cannot distinguish WHICH half of SR1 fired: a
*> screen that only checked the X"" prefix would accept this and silently fill with the first byte,
*> which is precisely the raw[0] behaviour the screen replaced.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB152B.
OPTIONS.
    INITIALIZE ALL TO X"5A5B".
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC X(4).
PROCEDURE DIVISION.
MAIN.
    DISPLAY A.
    STOP RUN.
