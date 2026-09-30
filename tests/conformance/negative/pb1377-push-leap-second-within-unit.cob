*> reject-at: 2023
*> ISO/IEC 1989:2023 7.3.22.3 SR2 (cite.py --check 7.3.22.3 "If directive-name is specified, the PUSH directive shall not be
*> specified where directive-name must not be specified." -> OK 2)) with 7.3.17.3 SR1: LEAP-SECOND must not be specified within a
*> compilation unit, so a PUSH naming it there is the same violation. kb/Work PB1377. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1377N01.
       PROCEDURE DIVISION.
       >>PUSH LEAP-SECOND
           DISPLAY "A".
           STOP RUN.
