*>--- conformance-case
*> id: pb732-value-word-alnum
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB732 — arm: Format 1 (data-item) VALUE, ALPHANUMERIC subject. THE SILENT HALF: pre-fix the
*> undefined word was promoted to an alphanumeric literal OF ITS OWN SPELLING, so this program compiled
*> clean, ran, and printed [NOSUCHW] with ZERO diagnostics. Same rule as the numeric arm: ISO 13.18.63.2
*> Format 1 writes literal-1, and a word is a literal only as a constant-name (13.10.3 SR2) or a
*> symbolic-character (8.3.3.6.2 Format 7) -> COBOLNET1639 (8.4.2.1).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB732B.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC X(7) VALUE NOSUCHW.
PROCEDURE DIVISION.
    DISPLAY A.
    STOP RUN.
