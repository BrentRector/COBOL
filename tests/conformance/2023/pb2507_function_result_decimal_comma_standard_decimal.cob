      *> kb/Work PB2507, the STANDARD-DECIMAL lane (8.8.1.5): the function's
      *> value is an SDIDI intermediate, rendered through CobolDec's own
      *> text entry rather than the fixed-point one, and the rule is the
      *> same one: the LITERAL FORM of the value (CONFORMANCE.md DOC-A.1-92)
      *> with the program's decimal separator, the comma under
      *> DECIMAL-POINT IS COMMA (12.3.7.4 GR14 a).  A second lane that
      *> forgot the clause would print periods here while the native lane
      *> (2023/pb2507_function_result_decimal_comma_native) printed commas.
      *>   MAX(X Y) = 2,25    ABS(N) = 1,50    SUM(X Y) = 3,75
      *>   NUMVAL("-1,5") = -1,5
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2507S.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9V99 VALUE 1,5.
       01 Y PIC 9V99 VALUE 2,25.
       01 N PIC S9V99 VALUE -1,5.
       PROCEDURE DIVISION.
           DISPLAY FUNCTION MAX(X Y)
           DISPLAY FUNCTION ABS(N)
           DISPLAY FUNCTION SUM(X Y)
           DISPLAY FUNCTION NUMVAL("-1,5")
           STOP RUN.
