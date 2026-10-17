*>--- conformance-case
*> id: pb977-special-names-for-trailing
*> rules: [FMT-12.3.7.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB977 - ISO 12.3.7.2 prints the FOR phrase in ONE position: "ALPHABET alphabet-name-2 FOR
*> NATIONAL IS ..." - after the name, before IS.  Written AFTER the definition it is a spelling no
*> edition prints and no dialect owns; it used to compile clean as a "historical superset" while the
*> same postfix on the CLASS clause drew a bare parse error.  Expected: COBOLNET2315 at every edition.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB977NEG.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET A2 IS NATIVE FOR NATIONAL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 AX PIC X(2) VALUE "12".
PROCEDURE DIVISION.
    DISPLAY AX
    STOP RUN.
