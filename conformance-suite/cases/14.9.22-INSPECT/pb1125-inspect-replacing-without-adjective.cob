*>--- conformance-case
*> id: pb1125-inspect-replacing-without-adjective
*> rules: [FMT-14.9.22.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1125 - ISO 14.9.22.2 general formats (rendered from the PDF, folio 643-644): CHARACTERS, ALL, LEADING and FIRST are required words of the replacing-phrase (one choice, all underlined),
      *> so a pair with no adjective is not a format.
      *> Expected: a syntax error (COBOL0001) at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1125G3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(6) VALUE "AABCDE".
       01 N PIC 99 VALUE 0.
       01 M PIC 99 VALUE 0.
       PROCEDURE DIVISION.
           INSPECT X REPLACING "A" BY "Z"
           DISPLAY X
           STOP RUN.
