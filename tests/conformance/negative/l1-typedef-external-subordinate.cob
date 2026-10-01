      *> reject-at: 2002 2014 2023
      *> ISO §13.18.58.3 SR3 — "If the TYPEDEF clause is specified with
      *> the EXTERNAL clause, the type declaration shall be at
      *> level-number 1."  T is written with BOTH clauses on a level-05
      *> entry under the open group G, so its level-number is not 1 and
      *> the source element shall be rejected.
      *> §13.16.3 SR15 ("The TYPEDEF clause may be specified only in a
      *> data description entry whose level-number is 1") and
      *> §13.18.22.3 SR1 (EXTERNAL only "in level 1 data description
      *> entries ... and in level 1 type declarations") forbid the same
      *> entry; this witness pins the COMBINATION SR3 names, which
      *> pb488-typedef-subordinate (TYPEDEF without EXTERNAL) does not.
      *> Expected diagnostic: COBOLNET1529 type-declaration-shape
      *> (docs/DIAGNOSTICS.md: "a type declaration at the wrong level or
      *> under another entry").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TDX01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T IS TYPEDEF IS EXTERNAL PIC X(3).
       PROCEDURE DIVISION.
           DISPLAY "X"
           STOP RUN.
