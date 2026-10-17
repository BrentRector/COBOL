*>--- conformance-case
*> id: func_expr_arg
*> rules: [GR-8.8.1.2-2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> P7 Step 12 probe - FUNCTION arguments are real arithmetic expressions (ISO 8.4.3.2 SR8):
      *> compound arguments with * / ** and parentheses (8.8.1; ** folds LEFT per 8.8.1.2 r3), the
      *> 8.7.1/8.3.3.3.2 space-vs-adjacent sign discrimination (MAX(A -4) = two args, MAX(A - 4) = one),
      *> table(ALL) expansion (15.3) beside expression args, and string-channel numeric arguments
      *> (division + numeric-edited de-edit through the ONE expression renderer - the deleted static
      *> channel could render neither).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FNEXPARG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(3) VALUE 12.
       01 B PIC 9(3) VALUE 4.
       01 C PIC 9(3) VALUE 5.
       01 ARR VALUE "40537".
           02 IND OCCURS 5 TIMES PIC 9.
      *> ⛔ THE NUMERIC-EDITED CASE THAT USED TO SIT HERE IS GONE, AND IS NOW A NEGATIVE
      *> FIXTURE (pb1-numeric-edited-arith-operand). It read
      *>     COMPUTE R = FUNCTION ORD(FUNCTION CHAR(WS-ED))   *> WS-ED PIC Z9
      *> and asserted that a numeric-edited item de-edits as an arithmetic operand. Owner
      *> decision 2026-08-02: it does not. 8.8.1.1 admits "an identifier referencing a
      *> NUMERIC data item"; 8.5.2.13 calls a numeric-edited item a "numeric-edited data
      *> item" - a distinct defined term - and 8.5.2.1 Table 2 puts that category in class
      *> ALPHANUMERIC or NATIONAL, never numeric. De-editing is granted by the MOVE rules
      *> (14.9.25.4 GR6d1) and nowhere extended to arithmetic. The rest of this golden is
      *> untouched: FUNCTION arguments ARE real arithmetic expressions (8.4.3.2 SR8), which
      *> is what it exists to pin.
       01 R PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MAX(A / B, C).
           DISPLAY "DIV=" R.
           COMPUTE R = FUNCTION MAX(A * B, (C + 1) / 2, 3 + 4).
           DISPLAY "CMPD=" R.
           COMPUTE R = FUNCTION MAX(2 ** 3 ** 2, 1).
           DISPLAY "POW=" R.
           COMPUTE R = FUNCTION MAX(A -4).
           DISPLAY "ADJ=" R.
           COMPUTE R = FUNCTION MAX(A - 4).
           DISPLAY "SUB=" R.
           COMPUTE R = FUNCTION MAX(-4 7).
           DISPLAY "SGN=" R.
           COMPUTE R = FUNCTION SUM(IND(ALL)) + FUNCTION MIN(A / B, 2).
           DISPLAY "ALL=" R.
           COMPUTE R = FUNCTION ORD(FUNCTION CHAR(66 / 2 + 1)).
           DISPLAY "ORD=" R.
           STOP RUN.
       END PROGRAM FNEXPARG.
