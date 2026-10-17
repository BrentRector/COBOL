*>--- conformance-case
*> id: alphabet-standard1-for-national
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.3.7.2 - STANDARD-1/STANDARD-2 name the ISO/IEC 646 ALPHANUMERIC coded character
*> set and appear only in the FOR ALPHANUMERIC branch of the alphabet-name-clause; the FOR NATIONAL
*> branch admits LOCALE / NATIVE / UCS-4 / UTF-8 / UTF-16 / code-name-2 / literal-phrase only.
*> The binder rejects the branch mismatch - COBOLNET0898.
IDENTIFICATION DIVISION.
PROGRAM-ID. ALPS1P10AN.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET AB1 FOR NATIONAL IS STANDARD-1.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
