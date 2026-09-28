      *> reject-at: 85 2002 2014 2023
      *> ISO 1989:2023 8.8.4.4.3 SR3: "If the alphabet-name-1, ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER,
      *> BOOLEAN, or class-name-1 phrase is specified, identifier-1 shall reference a data-item whose usage is
      *> display or national. If identifier-1 is a function-identifier, it shall reference an alphanumeric or
      *> national function." SQRT is a NUMERIC function (15.2 item 4), and alphabet-name-1 is named by SR3
      *> alone (SR4's list omits it), so this is the rule's function sentence speaking (kb/Work PB1401).
      *> COBOLNET2202.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1401NEGSR3.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS STANDARD-1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF FUNCTION SQRT(4) IS AL DISPLAY "IN-SET" END-IF
           STOP RUN.
