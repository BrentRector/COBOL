      *>--- conformance-case
      *> id: l1c01_arithmetic_clause_forms
      *> rules: [FMT-11.9.5.1]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §11.9.5.1 format — ARITHMETIC [IS] {NATIVE |
      *> STANDARD-DECIMAL}, IS optional, one alternative per clause
      *> THE FORMAT: ARITHMETIC IS {NATIVE | STANDARD-BINARY |
      *> STANDARD-DECIMAL}; ARITHMETIC and the three phrases are
      *> underlined, IS is NOT (optional); plain braces = exactly one.
      *>   cite.py --check 11.9.5.1 "STANDARD-DECIMAL"
      *>     -> OK §11.9.5.1 (General format)
      *>   cite.py --check 11.9.5.2 "If the STANDARD-DECIMAL phrase is
      *>     specified, the techniques used in handling arithmetic
      *>     expressions, arithmetic statements, the SUM clause, and
      *>     integer and numeric functions shall be as described for
      *>     standard-decimal arithmetic" -> OK §11.9.5.2 3)
      *>   cite.py --check 11.9.5.2 "If the ARITHMETIC clause is not
      *>     specified in this source element or a containing source
      *>     element" -> OK §11.9.5.2 4)
      *>   cite.py --check 15.84.4 "When standard-decimal arithmetic is
      *>     in effect, the returned value is the absolute value of the
      *>     exact square root of argument-1 rounded to 34 digits"
      *>     -> OK §15.84.4 2)
      *>   cite.py --check 15.84.4 "When native arithmetic is in effect,
      *>     the returned value is the absolute value of the
      *>     approximation of the square root" -> OK §15.84.4 4)
      *> STANDARD-BINARY is the documented processor-dependent decline
      *> (A.3 item 2, docs/CONFORMANCE.md, COBOLNET0806); not used here.
      *> 2014 dir: STANDARD-DECIMAL first exists in 2014.
      *> Each source element has its own OPTIONS paragraph (§11.9.5.2 4
      *> names "this source element or a containing source element");
      *> the contained programs override the outer program's clause.
      *> DERIVATION of every .out line:
      *>   sqrt(2) = 1.41421356237309504880168872420969807...; to 34
      *>   digits (the next digit is 0, so every rounding mode keeps it)
      *>   1.414213562373095048801688724209698; MOVE into 9V9(30)
      *>   truncates to 30 decimals -> 1414213562373095048801688724209.
      *>   OUTER  ARITHMETIC NATIVE (IS omitted): native approximation
      *>          is implementor-defined: only a 1E-6 window is pinned
      *>          -> OUTER-NATIVE WINDOW-OK
      *>   L1C01H1 ARITHMETIC STANDARD-DECIMAL (IS omitted)
      *>          -> SD-NOIS 1414213562373095048801688724209
      *>   L1C01H2 ARITHMETIC IS STANDARD-DECIMAL
      *>          -> SD-IS 1414213562373095048801688724209
      *>   L1C01H3 ARITHMETIC IS NATIVE -> NAT-IS WINDOW-OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C01H.
       OPTIONS.
           ARITHMETIC NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9V9(30).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W = FUNCTION SQRT(2)
           IF W >= 1.414213 AND W <= 1.414214
               DISPLAY "OUTER-NATIVE WINDOW-OK"
           ELSE
               DISPLAY "OUTER-NATIVE BAD " W
           END-IF
           CALL "L1C01H1"
           CALL "L1C01H2"
           CALL "L1C01H3"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C01H1.
       OPTIONS.
           ARITHMETIC STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9V9(30).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W = FUNCTION SQRT(2)
           DISPLAY "SD-NOIS " W
           GOBACK.
       END PROGRAM L1C01H1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C01H2.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9V9(30).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W = FUNCTION SQRT(2)
           DISPLAY "SD-IS " W
           GOBACK.
       END PROGRAM L1C01H2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C01H3.
       OPTIONS.
           ARITHMETIC IS NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9V9(30).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W = FUNCTION SQRT(2)
           IF W >= 1.414213 AND W <= 1.414214
               DISPLAY "NAT-IS WINDOW-OK"
           ELSE
               DISPLAY "NAT-IS BAD " W
           END-IF
           GOBACK.
       END PROGRAM L1C01H3.
       END PROGRAM L1C01H.
