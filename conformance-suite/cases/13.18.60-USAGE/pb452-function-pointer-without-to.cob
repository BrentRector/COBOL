*>--- conformance-case
*> id: pb452-function-pointer-without-to
*> rules: [GR-13.18.60.4-26, DOC-A.1-210]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.60.2 general format - FUNCTION-POINTER's `TO function-prototype-name-1` operand is
*> NOT bracketed (measured on the printed folio 503, where POINTER's and PROGRAM-POINTER's ARE), so the
*> phrase is REQUIRED and a bare USAGE FUNCTION-POINTER is nonconforming. 13.18.60.4 GR26 takes every
*> function-pointer's signature restriction from it. COBOLNET1958. kb/Work PB452.
*> (This file replaced `function-pointer-staged`, which pinned the COBOLNET0899 staged-loud non-support the
*> same PB452 landing removed - feedback_green_test_can_hold_a_gap_open.)
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGFP02.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 FP USAGE FUNCTION-POINTER.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
