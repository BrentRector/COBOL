*> reject-at: 2023
*> ISO/IEC 1989:2023 7.3.11.3 SR1 (cite.py --check 7.3.11.3 "Compilation-variable-name-1 shall not be the same as a
*> compiler-directive word." -> OK 1)); 8.12 lists the words (cite.py --check 8.12 "The following words are reserved in
*> compiler directives" -> OK) and reserves every exception-name of 14.6.13.1 in the context of compiler directives;
*> 7.3.3 SR9 reserves IMP. kb/Work PB1366. Fixed form.
*> `>>DEFINE EC-SIZE AS 1` names a compiler-directive word.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1366N04.
       PROCEDURE DIVISION.
       >>DEFINE EC-SIZE AS 1
           DISPLAY "A".
           STOP RUN.
