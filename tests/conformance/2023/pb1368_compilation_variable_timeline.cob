      *> kb/Work PB1368 - the compilation-variable table read OUTSIDE conditional compilation (ISO 7.3.11.4 GR1,
      *> 2023): "compilation-variable-name-1 may be used in the compilation group in any compiler directive where a
      *> literal of the category associated with the name is permitted, ... or in a constant entry where the FROM
      *> phrase is specified."
      *> (1) A COBOL-WORDS literal slot (7.3.10.3 SR2: each literal is alphanumeric): W1 and W2 reference
      *>     "DISPLAY" and "SHOW", so EQUATE W1 WITH W2 makes SHOW a synonym of DISPLAY (7.3.10.4 GR2).
      *> (2) PUSH / POP restore the table (7.3.22.4 GR3 "all instances", 7.3.20.4 GR1): V is 1, PUSH DEFINE saves
      *>     the table, V becomes 2 for CV2, POP DEFINE restores V = 1 for CV1.
      *> The negative half: negative/pb1368-cobol-words-numeric-variable (a numeric variable in an alphanumeric
      *> literal slot).
       >>DEFINE W1 AS "DISPLAY"
       >>DEFINE W2 AS "SHOW"
       >>COBOL-WORDS EQUATE W1 WITH W2
       >>DEFINE V AS 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1368TIMELINE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       >>PUSH DEFINE
       >>DEFINE V AS 2 OVERRIDE
       01 CV2 CONSTANT FROM V.
       >>POP DEFINE
       01 CV1 CONSTANT FROM V.
       PROCEDURE DIVISION.
           SHOW "CV1=" CV1 " CV2=" CV2
           STOP RUN.
