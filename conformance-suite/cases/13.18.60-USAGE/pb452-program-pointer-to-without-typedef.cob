*>--- conformance-case
*> id: pb452-program-pointer-to-without-typedef
*> rules: [SR-13.18.60.3-19, GR-13.18.60.4-25, SR-14.9.39.3-22]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.60.3 SR19 - "If program-prototype-name-1 is specified, the TYPEDEF clause shall be
*> specified for the subject of the entry." SR18 says the identical sentence about POINTER TO type-name-1
*> (pb153-pointer-to-type-without-typedef) and the two are enforced by ONE screen. A restricted
*> program-pointer is declared as a TYPE DECLARATION and reached by a TYPE clause - the conforming spelling
*> is the 2002 golden pb817_restricted_program_pointer. COBOLNET0881. kb/Work PB817 (and PB609's fix shape).
*> (This file replaced `program-pointer-restricted`, which pinned the COBOLNET0899 staged-loud non-support
*> the PB452/PB817 landing removed - feedback_green_test_can_hold_a_gap_open.)
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPP05.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
REPOSITORY.
    PROGRAM SOMEPROTO.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 PP USAGE PROGRAM-POINTER TO SOMEPROTO.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
