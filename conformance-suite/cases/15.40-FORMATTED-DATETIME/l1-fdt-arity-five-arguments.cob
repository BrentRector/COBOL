*>--- conformance-case
*> id: l1-fdt-arity-five-arguments
*> rules: [FMT-15.40.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.40.2's general format brackets exactly ONE trailing
      *> argument: FUNCTION FORMATTED-DATETIME ( argument-1 argument-2
      *> argument-3 [ argument-4 ] ). There is no argument-5, and no rule in
      *> §15.40.3 names one, so a fifth argument is not a reference to this
      *> function at any arity. The upper end of the bracket matters as much
      *> as the lower: a catalog row reading 3..5 would pass every line of
      *> 2023/l1_fdt_general_format and its two-argument sibling negative.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDTNEG3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(40).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss+hhmm"
               143951 45296 300 0) TO R
           STOP RUN.
