*>--- conformance-case
*> id: pb1034-not-less-or-equal-words
*> rules: [FMT-8.7.5.1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1034. ISO 1989:2023 8.8.4.2.2 Format 1: `IS LESS THAN OR EQUAL TO` has no [NOT] bracket
      *> (rendered page PDF p217), so NOT LESS THAN OR EQUAL TO is no alternative - the sibling of
      *> pb1034-not-greater-or-equal-words. (The START form is pb333-start-not-or-equal-operator.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1034NEG4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 5.
       01 B PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           IF A NOT LESS THAN OR EQUAL TO B
               DISPLAY "GT"
           ELSE
               DISPLAY "LE"
           END-IF
           STOP RUN.
