*>--- conformance-case
*> id: l1-fdt-argument1-not-a-literal
*> rules: [AR-15.40.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.40.3 r1 - "Argument-1 shall be a national or alphanumeric
      *> LITERAL." A PIC X(15) data item holding the very same characters is
      *> of the right CATEGORY and still is not a literal, which is the half
      *> of r1 a class screen cannot see. It matters because §15.40.3 r2's
      *> combined-format screen and §15.40.3 r6's zone screen are both
      *> decided at BIND time from the literal's content - a data item defers
      *> both to run time, where §15.40.2's own format offers no place to
      *> report them.
      *> The accept side of r1, both admitted literal categories, is the
      *> positive golden 2023/l1_fdt_argument1_literal_kinds.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDTNEG4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F PIC X(15) VALUE "YYYYMMDDThhmmss".
       01 R PIC X(40).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION FORMATTED-DATETIME(F 143951 45296) TO R
           STOP RUN.
