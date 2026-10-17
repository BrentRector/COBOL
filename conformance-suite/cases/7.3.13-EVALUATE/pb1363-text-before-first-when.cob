*>--- conformance-case
*> id: pb1363-text-before-first-when
*> rules: [FMT-7.3.13.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.13.2 general format: >> EVALUATE, one or more >> WHEN, an optional
*> >> WHEN OTHER, then >> END-EVALUATE (cite.py --check 7.3.13.2 "END-EVALUATE" -> OK).
*> Fixed form. kb/Work PB1363.
*> Text-1 belongs to a >> WHEN phrase: nothing is written between the EVALUATE and its first WHEN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363N12.
       PROCEDURE DIVISION.
       >>EVALUATE 1
           DISPLAY "STRAY".
       >>WHEN 1
           DISPLAY "W1".
       >>END-EVALUATE
           STOP RUN.
