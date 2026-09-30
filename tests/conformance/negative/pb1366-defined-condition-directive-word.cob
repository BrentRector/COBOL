*> reject-at: 2023
*> ISO/IEC 1989:2023 7.3.8.4.3 SR1 (cite.py --check 7.3.8.4.3 "Compilation-variable-name-1 shall not be the same as a
*> compiler-directive word." -> OK 1)): the same restriction on a defined condition. LISTING is a 8.12 word. kb/Work PB1366.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1366N06.
       PROCEDURE DIVISION.
       >>IF LISTING IS DEFINED
           DISPLAY "DEF".
       >>ELSE
           DISPLAY "UNDEF".
       >>END-IF
           STOP RUN.
