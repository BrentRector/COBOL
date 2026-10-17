*>--- conformance-case
*> id: alphabet-ucs4-for-alphanumeric
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.3.7.2 - UCS-4/UTF-8/UTF-16 name NATIONAL coded character sets and appear only
*> in the FOR NATIONAL branch of the alphabet-name-clause; with the FOR phrase omitted the
*> ALPHANUMERIC phrase is implied (12.3.7.3 SR13), whose branch admits LOCALE / NATIVE /
*> STANDARD-1 / STANDARD-2 / code-name-1 / literal-phrase only. The binder rejects the branch
*> mismatch - COBOLNET0898.
IDENTIFICATION DIVISION.
PROGRAM-ID. ALPU4P10AN.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    ALPHABET AB2 IS UCS-4.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
