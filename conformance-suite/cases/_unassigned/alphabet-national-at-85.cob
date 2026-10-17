*>--- conformance-case
*> id: alphabet-national-at-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.3.7.2 - the ALPHABET ... FOR NATIONAL branch and its UCS-4/UTF-8/UTF-16
*> coded-character-set phrases are COBOL-2002 introductions (the national class): at --std 85 the
*> version-conformance pass rejects on recognition (COBOLNET0900; registry rows
*> special-names-for-national-2002 + alphabet-national-2002). The 2002+ half of the contract is the
*> alphabet-national-2002 version-matrix row + the alphabet_national golden.
IDENTIFICATION DIVISION.
PROGRAM-ID. ALPN85P10AN.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET UNI FOR NATIONAL IS UCS-4.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
