*>--- conformance-case
*> id: pb225-float-class-below-2014
*> rules: [FMT-8.8.4.4.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 8.8.4.4.2's FLOAT-INFINITY alternative is a COBOL-2014 facility: 8.9 reserves the word
      *> only from 2014 (reserved-words.json r2002 false; Annex E, the 2014-to-2023 change list, does not name
      *> it), so below 2014 `IF X IS FLOAT-INFINITY` can only be the class-name-1 alternative - and with no
      *> SPECIAL-NAMES CLASS of that name the word identifies no resource (8.4.2.1), COBOLNET1639. The 2002
      *> positive twin, which DOES declare the class, is 2002/pb225_float_class_words_are_user_words
      *> (kb/Work PB225).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB225BELOW2014.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X1 PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF X1 IS FLOAT-INFINITY DISPLAY "INF" END-IF
           STOP RUN.
