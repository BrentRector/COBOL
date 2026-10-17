*>--- conformance-case
*> id: pb1367-define-off-override
*> rules: [FMT-7.3.11.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.11.2 general format (cite.py --check 7.3.11.2 "OVERRIDE" -> OK): >>DEFINE compilation-variable-name-1
*> AS { { arithmetic-expression-1 | boolean-expression-1 | literal-1 | PARAMETER } [ OVERRIDE ] | OFF }. kb/Work PB1367. Fixed form.
*> OVERRIDE qualifies the value alternative only: OFF is an alternative of the OUTER brace.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1367N02.
       PROCEDURE DIVISION.
       >>DEFINE X AS OFF OVERRIDE
           DISPLAY "A".
           STOP RUN.
