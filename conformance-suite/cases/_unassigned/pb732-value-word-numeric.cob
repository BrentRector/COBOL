*>--- conformance-case
*> id: pb732-value-word-numeric
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB732 — arm: Format 1 (data-item) VALUE, NUMERIC subject.
*> ISO 13.18.63.2 Format 1 is `VALUE IS literal-1`; the only WORDS a literal position admits are a
*> constant-name (13.10.3 SR2 - "constant-name-1 may be used anywhere that a format specifies a literal")
*> and a symbolic-character (8.3.3.6.2 Format 7). NOSUCHW is neither, so the operand identifies no
*> resource (8.4.2.1) -> COBOLNET1639. Pre-fix the word survived to the C# backend as an identifier that
*> does not exist (CS0103, exit 70 - an internal-error shape where a diagnostic belongs).
*> Edition-invariant: no edition of the VALUE clause has ever admitted a name in a literal position.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB732A.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC 9 VALUE NOSUCHW.
PROCEDURE DIVISION.
    DISPLAY A.
    STOP RUN.
