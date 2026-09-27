*> reject-at: 2002 2014 2023
*> kb/Work PB1230. ISO 1989:2023 13.10.3 SR9: "If constant-name-1
*> duplicates another constant-name, the specification of
*> arithmetic-expression-1, literal-1, data-name-1, data-name-2, or
*> compilation-variable-name-1 shall be the same as specified in the
*> other constant-name". The literal 5 and the expression 2 + 3 denote
*> one value but are two specifications; before PB1230 the check
*> compared the folded values and this program compiled (COBOLNET1547).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB1230.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 K CONSTANT AS 5.
01 K CONSTANT AS 2 + 3.
PROCEDURE DIVISION.
MAIN.
    DISPLAY K
    STOP RUN.
