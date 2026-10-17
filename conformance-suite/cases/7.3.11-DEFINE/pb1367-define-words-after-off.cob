*>--- conformance-case
*> id: pb1367-define-words-after-off
*> rules: [FMT-7.3.11.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.11.2 general format (cite.py --check 7.3.11.2 "OVERRIDE" -> OK): >>DEFINE compilation-variable-name-1
*> AS { { arithmetic-expression-1 | boolean-expression-1 | literal-1 | PARAMETER } [ OVERRIDE ] | OFF }. kb/Work PB1367. Fixed form.
*> OFF is the whole operand; a word after it is neither OVERRIDE nor an inline comment (7.3.3 SR3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1367N04.
       PROCEDURE DIVISION.
       >>DEFINE X AS OFF JUNK
           DISPLAY "A".
           STOP RUN.
