*> reject-at: 2023
*> ISO/IEC 1989:2023 8.12: "all of the exception-names specified in 14.6.13.1, Exception conditions, are reserved in the
*> context of compiler directives" (cite.py --check 8.12 "are reserved in the context of compiler directives" -> OK), so an
*> exception-name cannot be a compilation-variable-name in a defined condition (7.3.8.4.3 SR1). kb/Work PB1366.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1366N07.
       PROCEDURE DIVISION.
       >>IF EC-SIZE IS NOT DEFINED
           DISPLAY "NOT".
       >>END-IF
           STOP RUN.
