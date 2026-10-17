*>--- conformance-case
*> id: pb1363-end-evaluate-without-evaluate
*> rules: [FMT-7.3.13.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.13.2 general format: >> EVALUATE, one or more >> WHEN, an optional
*> >> WHEN OTHER, then >> END-EVALUATE (cite.py --check 7.3.13.2 "END-EVALUATE" -> OK).
*> Fixed form. kb/Work PB1363.
*> A >>END-EVALUATE with no open >>EVALUATE closes nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363N07.
       PROCEDURE DIVISION.
           DISPLAY "A".
       >>END-EVALUATE
           STOP RUN.
