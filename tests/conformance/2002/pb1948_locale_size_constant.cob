      *> kb/Work PB1948 - PICTURE ... LOCALE ... SIZE IS integer-1 (ISO 13.18.40.2 Format 2) names an
      *>   integer-n, so an integer constant-name stands there (13.10.3 SR2; 5.5 1; 13.10.4 GR1).
      *>   cite.py: OK  13.10.3 2)  (Syntax rules)
      *>   cite.py: OK  13.18.40.5 14)  (General rules)
      *> The program is conformance/2002/pb64t6_locale_size_ec (EC-LOCALE-SIZE, 13.18.40.5 r14 b) with the
      *> four SIZE integers written as the constants S8 = 8, S7 = 7, C11 = 11 and C10 = 10, so the SAME
      *> verdicts hold: SIZE 8 truncates only the four suppressed-zero spaces (silent), SIZE 7 truncates the
      *> nonzero '1' (the condition is raised and the MOVE is interrupted), SIZE 11 only the zero digit
      *> (silent) and SIZE 10 also the grouping ',' (raised).
       >>TURN EC-LOCALE-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1948LS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KS8  CONSTANT AS 8.
       01 KS7  CONSTANT AS 7.
       01 KC11 CONSTANT AS 11.
       01 KC10 CONSTANT AS 10.
       01 S8 PIC ZZZZZZ9.99 LOCALE IS US SIZE IS KS8.
       01 S7 PIC ZZZZZZ9.99 LOCALE IS US SIZE IS KS7.
       01 C11 PIC 9999999.99 LOCALE IS US SIZE IS KC11.
       01 C10 PIC 9999999.99 LOCALE IS US SIZE IS KC10.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-LOCALE-SIZE.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE 1234.50 TO S8
           DISPLAY "SILENT1=[" S8 "]"
           MOVE 1234.50 TO S7
           DISPLAY "AFTER1=[" S7 "]"
           MOVE 1234.50 TO C11
           DISPLAY "SILENT2=[" C11 "]"
           MOVE 1234.50 TO C10
           DISPLAY "AFTER2=[" C10 "]"
           STOP RUN.
