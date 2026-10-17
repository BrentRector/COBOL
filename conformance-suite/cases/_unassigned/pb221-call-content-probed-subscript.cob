*>--- conformance-case
*> id: pb221-call-content-probed-subscript
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.2.3.2 makes a subscript arithmetic-expression-1 and 8.8.1.1
      *> admits only "an identifier referencing a numeric data item, a numeric
      *> literal, the figurative constant ZERO" - so E(XE) with XE PIC X(4) is
      *> illegal, exactly as the byte-identical MOVE E(XE) TO R is.
      *> It compiled CLEAN here because CALL's BY CONTENT arm committed a
      *> ReferenceResolver.Probe's Place into the bound tree. A probe is a
      *> type-discriminating sniff and is deliberately side-effect-free, so it
      *> applies NO position screen; committing its Place therefore bypassed
      *> every screen at four call sites. One statement, one rule, two verdicts:
      *> the adjacent BY REFERENCE operand drew COBOLNET0844 and this one did
      *> not. kb/Work PB221.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB221N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XE PIC X(4) VALUE "0002".
       01 T.
          05 E PIC X OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABC" TO T
           CALL "PB221SUB" USING BY CONTENT E(XE)
           STOP RUN.
