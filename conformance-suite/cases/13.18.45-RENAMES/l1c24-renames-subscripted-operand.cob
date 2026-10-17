      *>--- conformance-case
      *> id: l1c24-renames-subscripted-operand
      *> rules: [SR-13.18.45.3-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.45.3 SR7 — RENAMES data-name-2 written subscripted
      *> "Data-name-2 and data-name-3 shall not be subscripted."
      *>   cite.py --check 13.18.45.3 "Data-name-2 and data-name-3
      *>   shall not be subscripted."
      *>     -> OK §13.18.45.3 7) (Syntax rules)
      *> R-A(1) is a well-formed subscripted reference to a table
      *> element of record R. SR7 forbids the subscript as such; the
      *> RENAMES operand position admits only a (qualified) data-name.
      *> Every well-formed subscripted reference names an item subject
      *> to an OCCURS clause, so SR3's OCCURS prohibition necessarily
      *> co-applies — SR7 cannot be isolated from SR3 by any source.
      *> The .err pins the subscript diagnostic (the operand is not a
      *> data-name because it carries a subscript), not an OCCURS one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C24C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 R-A PIC X(2) OCCURS 3 TIMES.
          05 R-B PIC X(2).
       66 X1 RENAMES R-A(1).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY R-B.
           STOP RUN.
