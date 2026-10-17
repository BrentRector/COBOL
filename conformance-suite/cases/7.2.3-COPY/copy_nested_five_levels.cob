*>--- conformance-case
*> id: copy_nested_five_levels
*> rules: [GR-7.2.3.4-7]
*> accept: [2023]
*> files: [r34lvl2.cpy, r34lvl3.cpy, r34lvl4.cpy, r34lvl5.cpy]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R34's legal half - 7.2.3.4 GR12: without REPLACING, library text may contain COPY
      *> statements and "the implementation shall support nesting of at least 5 levels, including
      *> the first COPY statement in the sequence". This chain is exactly five: this COPY ->
      *> r34lvl2 -> r34lvl3 -> r34lvl4 -> r34lvl5 (the declaring copybook). The ILLEGAL half -
      *> the same nesting UNDER a REPLACING phrase (GR10) - draws COBOLNET1640 and is pinned by
      *> CopyReplacingNestedCopyTests, unit-side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R34NEST5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY r34lvl2.
       PROCEDURE DIVISION.
           DISPLAY L5-VAR.
           STOP RUN.
