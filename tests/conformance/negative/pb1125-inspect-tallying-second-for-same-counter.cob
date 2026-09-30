      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1125 - ISO 14.9.22.2 general formats (rendered from the PDF, folio 643-644): the tallying-phrase prints ONE `identifier-2 FOR` per counter; a second FOR for the same counter is not a
      *> continuation of the phrase (the operands of ALL/LEADING continue, the FOR does not).
      *> Expected: a syntax error (COBOL0307) at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1125G1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(6) VALUE "AABCDE".
       01 N PIC 99 VALUE 0.
       01 M PIC 99 VALUE 0.
       PROCEDURE DIVISION.
           INSPECT X TALLYING N FOR ALL "A" FOR ALL "B"
           DISPLAY X
           STOP RUN.
