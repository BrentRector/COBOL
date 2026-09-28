      *> reject-at: 85 2002 2014 2023
      *> ISO 1989:2023 8.8.4.4.3 SR4: "ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER, or class-name-1 shall
      *> not be specified if the category of the data item referenced by identifier-1 is boolean, numeric, or
      *> numeric-edited." A numeric function is category numeric (8.5.2.12 item 6; 15.2 item 4). The screen
      *> used to classify a data-item reference only, so the function failed open and its value's TEXT was
      *> tested for letters (kb/Work PB1401). COBOLNET0844.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1401NEGSR4.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF FUNCTION SQRT(4) IS ALPHABETIC DISPLAY "ALPHA" END-IF
           STOP RUN.
