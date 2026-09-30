*> reject-at: 2023
*> ISO/IEC 1989:2023 7.3.25.2 general format, printed diagram (PDF page 115, rendered): CHECKING { ON [ WITH LOCATION ] | OFF } with
*> 7.3.25.3 SR1 (cite.py --check 7.3.25.3 "Any user-defined word that duplicates a compiler-directive word is interpreted as a
*> compiler-directive word rather than as file-name-1" -> OK 1)): ON is a 8.12 word, so after EC-I-O it is not file-name-1 and
*> the directive matches no format. kb/Work PB1365. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1365N01.
       PROCEDURE DIVISION.
       >>TURN EC-I-O ON CHECKING ON
           DISPLAY "A".
           STOP RUN.
