*>--- conformance-case
*> id: pb1363-evaluate-not-closed
*> rules: [SR-7.3.13.3-9, FMT-7.3.13.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.13.2 general format: >> EVALUATE, one or more >> WHEN, an optional
*> >> WHEN OTHER, then >> END-EVALUATE (cite.py --check 7.3.13.2 "END-EVALUATE" -> OK).
*> Fixed form. kb/Work PB1363.
*> The >>EVALUATE is never closed: >> END-EVALUATE is required.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363N11.
       PROCEDURE DIVISION.
       >>EVALUATE 1
       >>WHEN 1
           DISPLAY "W1".
           STOP RUN.
