      *> kb/Work PB2507, the native-arithmetic and folded lanes (the base
      *> cases are 85/pb2507_function_result_decimal_comma).  A numeric
      *> function's returned value used as text is the LITERAL FORM of the
      *> value (CONFORMANCE.md DOC-A.1-92, 15.4.1 / 14.9.11.4 GR1), and
      *> 12.3.7.4 GR14 a) makes the comma the character "written in
      *> numeric literals to represent the decimal separator" under
      *> DECIMAL-POINT IS COMMA.  Two lanes share the rule:
      *>   ABS is a run-time call: scale of the argument, so ABS(N) of an
      *>     S9V99 item holding -1,5 is 1,50; ABS(-1,5) is 1,5.
      *>   HIGHEST-ALGEBRAIC / LOWEST-ALGEBRAIC of a PIC 9V99 / S9V99 item
      *>     are FOLDED at compile time (15.43.4 r2 / 15.58.4 r2): 9,99
      *>     and -9,99, the literal form with the comma.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2507N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9V99 VALUE 1,5.
       01 N PIC S9V99 VALUE -1,5.
       PROCEDURE DIVISION.
           DISPLAY FUNCTION ABS(N)
           DISPLAY FUNCTION ABS(-1,5)
           DISPLAY FUNCTION HIGHEST-ALGEBRAIC(X)
           DISPLAY FUNCTION LOWEST-ALGEBRAIC(N)
           STOP RUN.
