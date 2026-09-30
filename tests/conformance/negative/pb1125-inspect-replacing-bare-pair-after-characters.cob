      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1125 - ISO 14.9.22.2 general formats (rendered from the PDF, folio 643-644): a CHARACTERS phrase repeats nothing, so a bare `literal BY literal` pair after it has no governing adjective
      *> (GR16's transitivity belongs to ALL, LEADING and FIRST).
      *> Expected: a syntax error (COBOL0307) at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1125G5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(6) VALUE "AABCDE".
       01 N PIC 99 VALUE 0.
       01 M PIC 99 VALUE 0.
       PROCEDURE DIVISION.
           INSPECT X REPLACING CHARACTERS BY "Z" "A" BY "B"
           DISPLAY X
           STOP RUN.
