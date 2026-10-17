*>--- conformance-case
*> id: alphabet-utf8-collating
*> rules: [SR-12.3.6.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.3.7 GR7 Table 6 - a UTF-8 (or UTF-16) alphabet references a CODED CHARACTER SET
*> but NOT a collating sequence (its collating-sequence column is empty); referencing it in
*> PROGRAM COLLATING SEQUENCE FOR NATIONAL violates 12.3.6 SR2 (alphabet-name-2 shall reference an
*> alphabet that defines a national collating sequence) - COBOLNET0898.
IDENTIFICATION DIVISION.
PROGRAM-ID. ALPU8P10AN.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
OBJECT-COMPUTER. XBOX-1
    PROGRAM COLLATING SEQUENCE FOR NATIONAL IS U8.
SPECIAL-NAMES.
    ALPHABET U8 FOR NATIONAL IS UTF-8.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
