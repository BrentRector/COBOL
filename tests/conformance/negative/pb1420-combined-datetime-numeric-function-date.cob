      *> reject-at: 2014 2023
      *> ISO 8.4.3.2.3 SR11 (cite.py --check 8.4.3.2.3 OK, rule 11) at 15.17.3 r1, "Argument-1 shall be in integer date
      *> form" (cite.py --check 15.17.3 OK), which 15.6 Table 21 types Int ("| COMBINED-DATETIME | Int1, Num2 | Num |"):
      *> a numeric function cannot be the integer date. Argument-2, typed Num (the standard numeric time form, which may
      *> carry fractional seconds), is unaffected: a numeric function there stays legal.
      *> kb/Work PB1420: the row typed both positions class numeric, so the integer-operand screen never ran on argument-1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1420NEG4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9(8)V9(5).
       PROCEDURE DIVISION.
           COMPUTE X = FUNCTION COMBINED-DATETIME(FUNCTION SQRT(4) 0)
           STOP RUN.
