*>--- conformance-case
*> id: pb1363-if-not-closed
*> rules: [SR-7.3.16.3-7, FMT-7.3.16.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.16.2 general format: >> IF, an optional >> ELSE, then >> END-IF
*> (cite.py --check 7.3.16.2 ">> END-IF" -> OK). Fixed form. kb/Work PB1363.
*> The >>IF is never closed: >> END-IF is not optional in the format.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1363N04.
       PROCEDURE DIVISION.
       >>DEFINE V AS 1
       >>IF V = 1
           DISPLAY "A".
           STOP RUN.
