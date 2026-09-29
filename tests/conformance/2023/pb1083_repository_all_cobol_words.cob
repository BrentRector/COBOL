       >>COBOL-WORDS UNDEFINE "SQRT"
       >>COBOL-WORDS EQUATE "PI" WITH "MYPI"
       >>COBOL-WORDS SUBSTITUTE "E" BY "EULER"
      *> FUNCTION ALL INTRINSIC AFTER >>COBOL-WORDS (kb/Work PB1083).
      *> ISO/IEC 1989:2023 §12.3.8.4 GR14: ALL is "as if each of the
      *> intrinsic-function-names defined in 8.11, Intrinsic function
      *> names, except for those that may have been undefined by the
      *> COBOL-WORDS directive, were specified"; a SUBSTITUTE literal-4
      *> is replaced in that list by its literal-5, and an EQUATE
      *> literal-2 is added to it. §12.3.8.3 SR13 then forbids only the
      *> names ON the list as user-defined words. So SQRT (undefined) and
      *> E (substituted away) are legal data-names here, and MYPI / EULER
      *> are intrinsic-function-names the REPOSITORY identifies, written
      *> without the word FUNCTION (§8.4.3.2.3 SR2): the approximations
      *> of pi and e (§15.73, §15.27). It used to refuse SQRT and E
      *> (COBOLNET1649) and not know MYPI or EULER (COBOLNET1639).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1083CW.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SQRT PIC 9 VALUE 7.
       01 E PIC 9 VALUE 8.
       PROCEDURE DIVISION.
           DISPLAY "SQRT=" SQRT
           DISPLAY "E=" E
           IF MYPI > 3.14159 AND MYPI < 3.14160
               DISPLAY "MYPI IS PI"
           ELSE
               DISPLAY "MYPI WRONG"
           END-IF
           IF EULER > 2.71828 AND EULER < 2.71829
               DISPLAY "EULER IS E"
           ELSE
               DISPLAY "EULER WRONG"
           END-IF
           STOP RUN.
