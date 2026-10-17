*>--- conformance-case
*> id: pb1125-inspect-tallying-first-operand-without-adjective
*> rules: [FMT-14.9.22.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1125 - ISO 14.9.22.2 general formats (rendered from the PDF, folio 643-644): CHARACTERS, ALL and LEADING are required words of the tallying-phrase, so the first operand after FOR
      *> needs its adjective (only the operands AFTER an ALL/LEADING operand are bare).
      *> Expected: a syntax error (COBOL0001) at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1125G2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(6) VALUE "AABCDE".
       01 N PIC 99 VALUE 0.
       01 M PIC 99 VALUE 0.
       PROCEDURE DIVISION.
           INSPECT X TALLYING N FOR "A"
           DISPLAY X
           STOP RUN.
