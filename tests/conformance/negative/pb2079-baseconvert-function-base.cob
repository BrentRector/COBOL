      *> reject-at: 2023
      *> kb/Work PB2079 - ISO/IEC 1989:2023 15.12.3 r1: "numeric integer literals or data items" - a nested
      *> function is neither a literal nor a data item, so FUNCTION INTEGER(16) is refused as a base
      *> (COBOLNET1627), as FIND-STRING refuses a function for its integer argument (15.37.3 r3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2079FUN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(40).
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT("1A" FUNCTION INTEGER(16) 10) TO R
           DISPLAY "R=" R
           STOP RUN.
