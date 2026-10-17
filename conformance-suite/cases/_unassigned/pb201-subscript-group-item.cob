*>--- conformance-case
*> id: pb201-subscript-group-item
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.2.3.2's subscript general format is exactly three alternatives -
      *> ALL, arithmetic-expression-1, index-name-1 [{+|-} integer-1] - so a bare
      *> data-name subscript is admitted ONLY as arithmetic-expression-1, and
      *> 8.8.1.1 then admits only "an identifier referencing a NUMERIC data item,
      *> a numeric literal, the figurative constant ZERO". 8.5.2.1: "an
      *> alphanumeric group item has class and category alphanumeric". So a GROUP
      *> is not admissible in the position, at every edition (8.8.1.1 is
      *> unchanged across 85/2002/2014/2023 - no introduction axis, no gate).
      *> kb/Work PB201: the strict REJECTION must survive the carrier reroute.
      *> The fast path can no longer render a group's record-struct carrier, so
      *> this now reaches ExpressionBinder on the D18 route - and the verdict has
      *> to be the SAME one, not a backend CS1503 and not silence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB201N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WG.
          05 WF1 PIC 9(2) VALUE 2.
       01 R  PIC X.
       01 T.
          05 E PIC X OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABC" TO T
           MOVE E(WG) TO R
           STOP RUN.
