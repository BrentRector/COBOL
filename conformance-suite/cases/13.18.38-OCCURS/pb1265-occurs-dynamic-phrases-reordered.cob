*>--- conformance-case
*> id: pb1265-occurs-dynamic-phrases-reordered
*> rules: [FMT-13.18.38.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1265 - ISO 5.2.1: "The words, phrases, clauses, punctuation, and operands in
      *> each general format shall be written in the compilation group in the sequence given in
      *> the general format"; Format 4 prints FROM before TO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1265NO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
           05 T PIC X OCCURS DYNAMIC TO 5 FROM 1.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
