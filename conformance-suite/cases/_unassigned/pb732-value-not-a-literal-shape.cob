*>--- conformance-case
*> id: pb732-value-not-a-literal-shape
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB732 — arm: a VALUE operand that is neither a word nor a literal. Every general format of
*> the VALUE clause writes literal-n (ISO 13.18.63.2), so a parenthesized arithmetic expression is not a
*> VALUE operand in any edition -> COBOLNET1902 (the general-format violation; nothing is undefined here,
*> which is why it is not COBOLNET1639). 4.2.2 is the obligation to indicate it: "An implementation shall
*> provide a warning mechanism ... to indicate violations of the general formats and the explicit syntax
*> rules of standard COBOL." Pre-fix the parenthesized text was handed to the C# backend verbatim.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB732I.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC 9(3) VALUE (7).
PROCEDURE DIVISION.
    DISPLAY A.
    STOP RUN.
