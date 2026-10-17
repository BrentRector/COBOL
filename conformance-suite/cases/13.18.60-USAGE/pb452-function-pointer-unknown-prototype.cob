*>--- conformance-case
*> id: pb452-function-pointer-unknown-prototype
*> rules: [GR-13.18.60.4-26]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 8.4.6.6, Scope of function-prototype-names - "Function-prototype-names referenced within a
*> source element shall be either the user-function-name of the containing function definition or a
*> function-prototype-name declared in the REPOSITORY paragraph." NOSUCHPROTO is neither, so 13.18.60.4
*> GR26's signature restriction would name nothing checkable. COBOLNET1958. kb/Work PB452.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGFP03.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 FP USAGE FUNCTION-POINTER TO NOSUCHPROTO.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
