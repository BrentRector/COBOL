      *>--- conformance-case
      *> id: pb1400-group-usage-arithmetic-operand
      *> rules: [GR-8.5.2.5-3, GR-8.5.2.10-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1400 — ISO 8.8.1.1: "An arithmetic expression may be
      *>   an identifier referencing a numeric data item, a numeric
      *>   literal, the figurative constant ZERO".
      *> A GROUP-USAGE BIT group is class boolean and a GROUP-USAGE
      *> NATIONAL group class national (8.5.2.1; 8.5.2.5 3);
      *> 8.5.2.10 3)), so neither is an arithmetic operand: each COMPUTE
      *> below draws COBOLNET0844, whose text names the 8.5.2.1 class of
      *> the group (it once called every group "class alphanumeric";
      *> ArithmeticOperandClassTests pins the text). The positive half
      *> is 2002/pb1400_group_usage_operand_class. GROUP-USAGE is
      *> COBOL-2002, so 85 refuses the clause itself instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1400NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GB GROUP-USAGE BIT.
          05 GB1 PIC 1(4) VALUE B"1010".
       01 GN GROUP-USAGE NATIONAL.
          05 GN1 PIC N(2).
       01 N PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE N = GB + 1.
           COMPUTE N = GN + 1.
           STOP RUN.
