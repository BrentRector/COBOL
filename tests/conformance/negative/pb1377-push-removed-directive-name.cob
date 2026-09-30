*> reject-at: 2023
*> ISO/IEC 1989:2023 7.3.22.3 SR1 (cite.py --check 7.3.22.3 "Directive-name shall be the name of a compiler directive other than
*> an EVALUATE, IF, PAGE, POP, or PUSH directive." -> OK 1)) with Annex E.2 item 21, which REMOVED FLAG-85 at 2023 (cite.py --check E.2
*> "FLAG-85" -> OK 21)): FLAG-85 names no compiler directive at 2023, the only edition with
*> PUSH. kb/Work PB1377. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1377N04.
       PROCEDURE DIVISION.
       >>PUSH FLAG-85
           DISPLAY "A".
           STOP RUN.
