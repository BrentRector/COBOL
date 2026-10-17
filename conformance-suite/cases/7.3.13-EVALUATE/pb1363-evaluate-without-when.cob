*>--- conformance-case
*> id: pb1363-evaluate-without-when
*> rules: [SR-7.3.13.3-9, FMT-7.3.13.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.13.2 general format: >> EVALUATE, one or more >> WHEN, an optional
*> >> WHEN OTHER, then >> END-EVALUATE (cite.py --check 7.3.13.2 "END-EVALUATE" -> OK).
*> Fixed form. kb/Work PB1363.
*> The >> WHEN group is required ({ >> WHEN ... } ...): one or more.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363N10.
       PROCEDURE DIVISION.
       >>EVALUATE 1
       >>END-EVALUATE
           DISPLAY "A".
           STOP RUN.
