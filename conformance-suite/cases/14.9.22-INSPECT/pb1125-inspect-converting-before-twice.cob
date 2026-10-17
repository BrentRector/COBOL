*>--- conformance-case
*> id: pb1125-inspect-converting-before-twice
*> rules: [FMT-14.9.22.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1125 - ISO 14.9.22.2 general formats (rendered from the PDF, folio 643-644): the after-before-phrase's choice indicators allow each of BEFORE and AFTER at most once; CONVERTING used a
      *> separate repeatable rule where the last BEFORE silently won.
      *> Expected: a syntax error (COBOL0307) at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1125G4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(6) VALUE "AABCDE".
       01 N PIC 99 VALUE 0.
       01 M PIC 99 VALUE 0.
       PROCEDURE DIVISION.
           INSPECT X CONVERTING "ABCDEF" TO "abcdef" BEFORE "E" BEFORE "C"
           DISPLAY X
           STOP RUN.
