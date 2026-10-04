*> reject-at: 2002 2014 2023
*> kb/Work PB1948 - ISO 12.3.7.3 SR16 e) 2.: "When the IN phrase is not specified, the ordinal position specified
*>   by integer-1 shall exist in the native alphanumeric character set"; 13.10.4 GR1 makes the constant
*>   KBIG "as if" its literal were written, and 13.10.3 SR2 lets it stand as the ordinal. KBIG = 70000 is past
*>   the 65,536 characters of this compiler's native set (UTF-16 code units; docs/CONFORMANCE.md DOC-A.1-188),
*>   so the clause is refused (COBOLNET1670) exactly as SYMBOLIC CHARACTERS SC-A IS 70000 is. The word-only
*>   spelling is used (no IS/ARE), so the binder must first tell the name SC-A from the constant KBIG.
*>   cite.py: OK  12.3.7.3 16)  (Syntax rules)
*>     ("the ordinal position specified by integer-1 shall exist in the native alphanumeric character set")
*>   cite.py: OK  13.10.3 2)  (Syntax rules)
*> The positive twin is conformance/2002/pb1948_integer_n_constants.cob.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1948N4.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    SYMBOLIC CHARACTERS SC-A KBIG.
DATA DIVISION.
WORKING-STORAGE SECTION.
01  KBIG CONSTANT AS 70000.
PROCEDURE DIVISION.
MAIN-PARA.
    DISPLAY SC-A
    STOP RUN.
