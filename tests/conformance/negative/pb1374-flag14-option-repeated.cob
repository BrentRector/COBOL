      *> reject-at: 2023
      *> kb/Work PB1374 - ISO 1989:2023 5.2.6.4: "When enclosed by braces, one or more of the alternatives contained
      *> within the choice indicators shall be specified, but any single alternative shall be specified only once."
      *> The option words of 7.3.14.2 / 7.3.15.2 sit inside choice indicators enclosed by braces, so a repeated option word
      *> is a syntax error (COBOLNET1622). It was silently merged before PB1374, which hid a typo for the
      *> option the author meant.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1374N14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       >>FLAG-14 VALUE-ZERO VALUE-ZERO ON
       01 R PIC 9 VALUE 1.
       PROCEDURE DIVISION.
           STOP RUN.
