*>--- conformance-case
*> id: pb170-subscript-alphanumeric-simple
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.2.3.2's subscript general format is exactly three alternatives -
      *> ALL, arithmetic-expression-1, index-name-1 [{+|-} integer-1] - so a bare
      *> data-name subscript is admitted ONLY as arithmetic-expression-1, and
      *> 8.8.1.1 then admits only "an identifier referencing a NUMERIC data item,
      *> a numeric literal, the figurative constant ZERO". 8.5.2.1 Table 2 puts
      *> category alphanumeric in class ALPHANUMERIC.
      *> THE ARM PB155 COULD NOT REACH: a SIMPLE subscript name resolves in
      *> ReferenceResolver.ResolveSubscriptName and renders CobolTable.Occ(...)
      *> straight from the token, so the expression binder's screen never ran.
      *> Measured on 9a89fbd1: this program compiled clean and printed R=B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB170N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XE PIC X(4) VALUE "0002".
       01 R  PIC X.
       01 T.
          05 E PIC X OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABC" TO T
           MOVE E(XE) TO R
           STOP RUN.
