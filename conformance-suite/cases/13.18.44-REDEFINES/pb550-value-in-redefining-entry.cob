*>--- conformance-case
*> id: pb550-value-in-redefining-entry
*> rules: [SR-13.18.44.3-9, SR-13.18.63.3-12]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.63.3 SR12 - "The VALUE clause shall not be specified in a data description entry that
*> contains a REDEFINES clause or in an entry that is subordinate to an entry containing a REDEFINES clause."
*> A syntax rule at every edition (Annex E records no change). MEASURED BEFORE: compiled clean and displayed
*> [AAAA|AAAA] - B's VALUE was silently discarded, because a REDEFINES view never seeds the storage it shares
*> with its anchor (kb/Work PB550). The subordinate, group-level and format-2 (SR16) spellings are pinned by
*> ValueClauseScreenTests. (COBOLNET2406)
IDENTIFICATION DIVISION.
PROGRAM-ID. PB550REDVAL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 R.
    05 A PIC X(4) VALUE "AAAA".
    05 B REDEFINES A PIC X(4) VALUE "ZZZZ".
PROCEDURE DIVISION.
MAIN.
    DISPLAY "[" A "|" B "]"
    STOP RUN.
