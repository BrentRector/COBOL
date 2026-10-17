*>--- conformance-case
*> id: pb452-function-pointer-at-2002
*> rules: [GR-13.18.60.4-26]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.60.4 GR26 / 8.5.2.7 - USAGE FUNCTION-POINTER is a COBOL-2014 introduction
*> (reserved-words.json FUNCTION-POINTER r2002=false r2014=true), so below 2014 the version-conformance pass
*> rejects it: COBOLNET0900, registry row usage-function-pointer-2014. The EDITION GATE is the half a
*> feature landing must re-measure - the same source compiles clean at 2014 and 2023 (the positive golden
*> conformance:2014/pb452_set_function_pointer). kb/Work PB452.
IDENTIFICATION DIVISION.
FUNCTION-ID. NEGFPPRO IS PROTOTYPE.
DATA DIVISION.
LINKAGE SECTION.
01 L-R PIC 9(4).
PROCEDURE DIVISION RETURNING L-R.
END FUNCTION NEGFPPRO.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGFP01.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    FUNCTION NEGFPPRO.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 FP USAGE FUNCTION-POINTER TO NEGFPPRO.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
END PROGRAM NEGFP01.
