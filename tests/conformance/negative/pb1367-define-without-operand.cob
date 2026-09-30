*> reject-at: 2002 2014 2023
*> ISO/IEC 1989:2023 7.3.11.2 general format (cite.py --check 7.3.11.2 "OVERRIDE" -> OK): >>DEFINE compilation-variable-name-1
*> AS { { arithmetic-expression-1 | boolean-expression-1 | literal-1 | PARAMETER } [ OVERRIDE ] | OFF }. kb/Work PB1367. Fixed form.
*> One alternative of the outer brace shall be selected: a name with no operand matches no format.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1367N03.
       PROCEDURE DIVISION.
       >>DEFINE X AS
           DISPLAY "A".
           STOP RUN.
