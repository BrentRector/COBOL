*>--- conformance-case
*> id: pb1363-else-without-if
*> rules: [SR-7.3.16.3-7, FMT-7.3.16.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.16.2 general format: >> IF, an optional >> ELSE, then >> END-IF
*> (cite.py --check 7.3.16.2 ">> END-IF" -> OK). Fixed form. kb/Work PB1363.
*> A >>ELSE with no open >>IF belongs to no directive.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363N01.
       PROCEDURE DIVISION.
           DISPLAY "A".
       >>ELSE
           DISPLAY "B".
           STOP RUN.
