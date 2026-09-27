      *> kb/Work PB1230. ISO 13.10.4 GR1: if literal-1 is specified, "the
      *> effect of specifying constant-name-1 in other than this entry is
      *> as if literal-1 ... were written where constant-name-1 is
      *> written" - literal-1 AS WRITTEN, its sign and its decimal
      *> separator included. Under DECIMAL-POINT IS COMMA (12.3.7.4 GR14a)
      *> 1,5 is the legal spelling; before PB1230 the constant carried the
      *> evaluator's normalized text '1.5', every reference re-checked
      *> that as a literal, and COBOLNET0895 refused this program. AS +5
      *> displayed '5' where the literal +5 displays '+5'.
      *> 13.10.3 SR9: a duplicated constant-name is legal when its
      *> specification is the same - K2 is written twice, differing only
      *> in separator spacing and letter case, which the text-word
      *> comparison (7.2.3.4 9) c)) does not distinguish.
      *> Expected values: each constant line equals the line for the
      *> literal written in its place (GR1); KC + 1 is 2,5 truncated to 2
      *> (13.10.4 GR4, 7.3.6.3 GR3); KC * 2 = 3,0; X(KI) is X(3) and
      *> OCCURS KI is OCCURS 3 (13.10.3 SR2 - KI is an integer).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1230P1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  KP CONSTANT AS +5.
       01  KC CONSTANT AS 1,5.
       01  KI CONSTANT AS +3.
       01  K2 CONSTANT AS KC + 1.
       01  k2 CONSTANT AS kc   +   1.
       01  T  PIC X(KI) VALUE "ABC".
       01  TB.
           05  E PIC X OCCURS KI.
       01  N  PIC 9V9 VALUE KC.
       01  NE PIC 9,9.
       01  M  PIC 9V9.
       PROCEDURE DIVISION.
           DISPLAY KP
           DISPLAY +5
           MOVE N TO NE
           DISPLAY NE
           MOVE KC TO NE
           DISPLAY NE
           MOVE 1,5 TO NE
           DISPLAY NE
           COMPUTE M = KC * 2
           MOVE M TO NE
           DISPLAY NE
           DISPLAY K2
           DISPLAY T
           MOVE "XYZ" TO TB
           DISPLAY E (KI)
           IF KC = 1,5 DISPLAY "EQ" ELSE DISPLAY "NE" END-IF
           STOP RUN.
