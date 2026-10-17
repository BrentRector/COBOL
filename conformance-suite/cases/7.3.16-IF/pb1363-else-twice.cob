*>--- conformance-case
*> id: pb1363-else-twice
*> rules: [SR-7.3.16.3-7, FMT-7.3.16.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.16.2 general format: >> IF, an optional >> ELSE, then >> END-IF
*> (cite.py --check 7.3.16.2 ">> END-IF" -> OK). Fixed form. kb/Work PB1363.
*> The format writes [ >> ELSE [ text-2 ] ] - at most one per IF directive.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363N03.
       PROCEDURE DIVISION.
       >>DEFINE V AS 1
       >>IF V = 2
           DISPLAY "A".
       >>ELSE
           DISPLAY "B".
       >>ELSE
           DISPLAY "C".
       >>END-IF
           STOP RUN.
