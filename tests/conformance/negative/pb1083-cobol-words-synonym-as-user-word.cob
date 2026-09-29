      *> reject-at: 2023
       >>COBOL-WORDS EQUATE "PI" WITH "MYPI"
       >>COBOL-WORDS SUBSTITUTE "E" BY "EULER"
      *> ISO/IEC 1989:2023 §12.3.8.4 GR14 (kb/Work PB1083): under
      *> FUNCTION ALL INTRINSIC an EQUATE literal-2 is ADDED to the list of
      *> intrinsic function names and a SUBSTITUTE literal-5 REPLACES its
      *> literal-4 there, so MYPI and EULER are intrinsic-function-names
      *> the REPOSITORY identifies and §12.3.8.3 SR13 forbids them as
      *> data-names. They used to compile, the data item shadowing the
      *> function. COBOLNET1649.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1083SY.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MYPI PIC 9 VALUE 7.
       01 EULER PIC 9 VALUE 8.
       PROCEDURE DIVISION.
           DISPLAY MYPI EULER
           STOP RUN.
