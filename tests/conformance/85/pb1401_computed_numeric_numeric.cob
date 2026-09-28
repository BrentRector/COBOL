      *> kb/Work PB1401 / PB1466 - NUMERIC over a COMPUTED category-numeric operand, at the edition
      *> that introduced the operands (the 2014 alternatives are pinned by
      *> 2014/pb1401_computed_numeric_class). ISO 1989:2023 8.5.2.12 items 3-7 make LINAGE-COUNTER,
      *> LINE-COUNTER, PAGE-COUNTER and every numeric / integer function a data item of category
      *> numeric, so 8.8.4.4.3 SR8 admits them and 8.8.4.4.4 GR3 n) 1. - not n) 2.'s all-digits
      *> test of their TEXT - decides: "the content ... consists entirely of a valid representation
      *> for the usage" (n) 1. c.). A register or a function result IS a value its carrier holds,
      *> so every NUMERIC leg is TRUE and every NOT NUMERIC leg FALSE. Before the fix the register
      *> legs threw NotImplemented at run time ("computed expression in a string context") and
      *> NUMVAL("1.5") / INTEGER(-5.5) answered FALSE on the text "1.5" / "-6". UPPER-CASE is an
      *> ALPHANUMERIC function, so n) 2. still decides it: "12" is all digits - TRUE, "1A" - FALSE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1401NUM85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFILE ASSIGN TO "pb1401-85-linage.tmp".
           SELECT RFILE ASSIGN TO "pb1401-85-report.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD PFILE LINAGE IS 10 LINES.
       01 PR PIC X(10).
       FD RFILE REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01 N PIC S9(3)V9 VALUE -5.5.
       REPORT SECTION.
       RD RPT PAGE LIMIT IS 20 LINES FIRST DETAIL 1.
       01 D-1 TYPE DETAIL.
          02 LINE 1 COLUMN 1 PIC X VALUE "X".
       PROCEDURE DIVISION.
           OPEN OUTPUT PFILE RFILE.
           WRITE PR FROM "A"
           IF LINAGE-COUNTER IS NUMERIC
               DISPLAY "L-NUM T" ELSE DISPLAY "L-NUM F" END-IF
           IF LINAGE-COUNTER IS NOT NUMERIC
               DISPLAY "L-NOTNUM T" ELSE DISPLAY "L-NOTNUM F" END-IF
           INITIATE RPT
           IF PAGE-COUNTER IS NUMERIC
               DISPLAY "P-NUM T" ELSE DISPLAY "P-NUM F" END-IF
           IF LINE-COUNTER IS NOT NUMERIC
               DISPLAY "LC-NOTNUM T" ELSE DISPLAY "LC-NOTNUM F" END-IF
           TERMINATE RPT
           CLOSE PFILE RFILE
           IF FUNCTION NUMVAL("1.5") IS NUMERIC
               DISPLAY "NV-NUM T" ELSE DISPLAY "NV-NUM F" END-IF
           IF FUNCTION INTEGER(N) IS NUMERIC
               DISPLAY "INT-NUM T" ELSE DISPLAY "INT-NUM F" END-IF
           IF FUNCTION SQRT(4) IS NOT NUMERIC
               DISPLAY "SQ-NOTNUM T" ELSE DISPLAY "SQ-NOTNUM F" END-IF
           IF FUNCTION UPPER-CASE("12") IS NUMERIC
               DISPLAY "UC-12 T" ELSE DISPLAY "UC-12 F" END-IF
           IF FUNCTION UPPER-CASE("1a") IS NUMERIC
               DISPLAY "UC-1A T" ELSE DISPLAY "UC-1A F" END-IF
           STOP RUN.
