*>--- conformance-case
*> id: pb1496-free-form-line-256
*> rules: [SR-6.1-3, DOC-A.1-157]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 6.1 3) a): a free-form line has at most 255 character positions.
*> The DISPLAY line below has 256 (kb/Work PB1496: accepted silently).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1496-NEG-256.
PROCEDURE DIVISION.
DISPLAY "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx".
STOP RUN.
