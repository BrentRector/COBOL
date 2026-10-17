*>--- conformance-case
*> id: pb225_float_class_words_are_user_words
*> rules: [FMT-8.8.4.4.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 8.8.4.4.2 offers seven numeric-content class alternatives from COBOL-2014 (kb/Work PB225);
      *> 8.9 reserves their words only from 2014 (Annex E, the 2014-to-2023 change list, names none of them, and
      *> tests/version-matrix/reserved-words.json carries r2002 false / r2014 true). So at COBOL-2002 each word is
      *> an ordinary user-defined word: here a SPECIAL-NAMES class-name (12.3.7), and `IF X IS FLOAT-INFINITY`
      *> is the class-name-1 alternative, true when X consists only of that class's characters (8.8.4.4.4 GR3 f).
      *> FLOAT-NOT-A-NUMBER-QUIET is the word with a lexer token of its own since PB225 - it must stay a user word
      *> below 2014 exactly as its six siblings do. The 2014 twin is 2014/pb225_float_class_conditions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB225USERCLASS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS FLOAT-INFINITY IS "I"
           CLASS FLOAT-NOT-A-NUMBER-QUIET IS "Q"
           CLASS NEAREST-TO-ZERO IS "0".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3) VALUE "III".
       01 Y PIC X(2) VALUE "QZ".
       01 Z PIC X(2) VALUE "00".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF X IS FLOAT-INFINITY DISPLAY "X-INF=Y" ELSE DISPLAY "X-INF=N" END-IF
           IF Y IS FLOAT-NOT-A-NUMBER-QUIET DISPLAY "Y-Q=Y" ELSE DISPLAY "Y-Q=N" END-IF
           IF Z IS NEAREST-TO-ZERO DISPLAY "Z-0=Y" ELSE DISPLAY "Z-0=N" END-IF
           STOP RUN.
