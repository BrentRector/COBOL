*> reject-at: 2023
*> ISO/IEC 1989:2023 7.3.25.2 general format, printed diagram (PDF page 115, rendered): CHECKING { ON [ WITH LOCATION ] | OFF } with
*> OFF has no WITH LOCATION alternative: { ON [ WITH LOCATION ] | OFF }. kb/Work PB1365. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1365N02.
       PROCEDURE DIVISION.
       >>TURN EC-SIZE CHECKING OFF WITH LOCATION
           DISPLAY "A".
           STOP RUN.
