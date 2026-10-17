*>--- conformance-case
*> id: pb1414_inspect_low_value_follows_program_collating_sequence
*> rules: [GR-8.3.3.6.4-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB1414 - ISO 8.3.3.6.4 GR7: at run time LOW-VALUE is the character with the lowest
      *>   ordinal position in the runtime collating sequence. PROGRAM COLLATING SEQUENCE AL
      *>   orders "B" first, so INSPECT sees LOW-VALUE as "B" (was the native U+0000).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1414P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE IS AL.
       SPECIAL-NAMES.
           ALPHABET AL IS "B" "A" "C".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T   PIC X(5) VALUE "ABBCB".
       01  N   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           INSPECT T TALLYING N FOR ALL LOW-VALUE
           DISPLAY N
           INSPECT T REPLACING ALL LOW-VALUE BY "Z"
           DISPLAY T
           STOP RUN.
