      *> kb/Work PB1401 / PB1466 - the class conditions over a COMPUTED category-numeric operand.
      *> ISO 1989:2023 8.5.2.12 items 3-7: "Each of the following is a data item of category
      *> numeric: ... 3) A LINE-COUNTER. 4) A LINAGE-COUNTER. 5) A PAGE-COUNTER. 6) A numeric
      *> function. 7) An integer function." 8.8.4.4.3 SR6/SR8 therefore admit each of them to
      *> NUMERIC, FARTHEST-FROM-ZERO, NEAREST-TO-ZERO and IN-ARITHMETIC-RANGE (COBOL-2014).
      *> Every leg used to fail: the three SR6 phrases crashed the COMPILER (a null field), and
      *> NUMERIC over a register threw NotImplemented at run time, over NUMVAL("1.5") and
      *> INTEGER(-5.5) answered FALSE (n) 2.'s all-digits test applied to the TEXT "1.5"/"-6").
      *> Expected values, derived:
      *>  NUMERIC (8.8.4.4.4 GR3 n) 1. c.): a register or a function result IS a value its carrier
      *>   holds - TRUE (L-NUM, P-NUM, NV-NUM, INT-NUM, SQ-NUM, RND-NUM); NOT NUMERIC - FALSE.
      *>   UPPER-CASE("12") is category ALPHANUMERIC, so n) 2.'s all-digits test still decides: TRUE.
      *>  NEAREST-TO-ZERO / FARTHEST-FROM-ZERO (GR3 m)/g)) against the capacity of the item the
      *>   operand references: LINAGE-COUNTER's size "is equal to the page size specified in the
      *>   LINAGE clause" (8.4.3.14.4 GR1) = 3 here, OPEN OUTPUT sets it to 1 (13.18.34.4 GR7):
      *>   NTZ before any WRITE is TRUE, after one WRITE (value 2) NTZ and FFZ are FALSE, after
      *>   a second WRITE (value 3) FFZ is TRUE. PAGE-COUNTER is 1 after INITIATE (NTZ TRUE);
      *>   LINE-COUNTER is not at its PIC 9(18) capacity (FFZ FALSE) - CONFORMANCE.md, the counter
      *>   registers' declared capacity. A function's temporary (CONFORMANCE.md DOC-A.1-92):
      *>   an INTEGER function has no fraction digits (15.2 item 5), so ABS(-1) IS NEAREST-TO-ZERO
      *>   is TRUE and INTEGER(-5.5) = -6 is not; SQRT(4) = 2 is a binary64, neither binary64
      *>   extreme - FALSE for both.
      *>  IN-ARITHMETIC-RANGE (GR3 l)): every value here is within the intermediate - TRUE.
      *>  The RANDOM leg: the class test evaluates the function ONCE, so the value drawn after it
      *>   is the THIRD of the seeded sequence, as in the reference draw (RND-SEQ).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1401CNC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFILE ASSIGN TO "pb1401-linage.tmp".
           SELECT RFILE ASSIGN TO "pb1401-report.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD PFILE LINAGE IS 3 LINES.
       01 PR PIC X(10).
       FD RFILE REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01 N PIC S9(3)V9 VALUE -5.5.
       01 R1 COMP-2.
       01 R3 COMP-2.
       01 RX COMP-2.
       REPORT SECTION.
       RD RPT PAGE LIMIT IS 20 LINES FIRST DETAIL 1.
       01 D-1 TYPE DETAIL.
          02 LINE 1 COLUMN 1 PIC X VALUE "X".
       PROCEDURE DIVISION.
           OPEN OUTPUT PFILE RFILE.
           IF LINAGE-COUNTER IS NEAREST-TO-ZERO
               DISPLAY "L-NTZ-1 T" ELSE DISPLAY "L-NTZ-1 F" END-IF
           WRITE PR FROM "A"
           IF LINAGE-COUNTER IS NUMERIC
               DISPLAY "L-NUM T" ELSE DISPLAY "L-NUM F" END-IF
           IF LINAGE-COUNTER IS NOT NUMERIC
               DISPLAY "L-NOTNUM T" ELSE DISPLAY "L-NOTNUM F" END-IF
           IF LINAGE-COUNTER IS IN-ARITHMETIC-RANGE
               DISPLAY "L-IAR T" ELSE DISPLAY "L-IAR F" END-IF
           IF LINAGE-COUNTER IS NEAREST-TO-ZERO
               DISPLAY "L-NTZ-2 T" ELSE DISPLAY "L-NTZ-2 F" END-IF
           IF LINAGE-COUNTER IS FARTHEST-FROM-ZERO
               DISPLAY "L-FFZ-2 T" ELSE DISPLAY "L-FFZ-2 F" END-IF
           WRITE PR FROM "B"
           IF LINAGE-COUNTER IS FARTHEST-FROM-ZERO
               DISPLAY "L-FFZ-3 T" ELSE DISPLAY "L-FFZ-3 F" END-IF
           INITIATE RPT
           IF PAGE-COUNTER IS NUMERIC
               DISPLAY "P-NUM T" ELSE DISPLAY "P-NUM F" END-IF
           IF PAGE-COUNTER IS NOT NUMERIC
               DISPLAY "P-NOTNUM T" ELSE DISPLAY "P-NOTNUM F" END-IF
           IF PAGE-COUNTER IS NEAREST-TO-ZERO
               DISPLAY "P-NTZ T" ELSE DISPLAY "P-NTZ F" END-IF
           IF LINE-COUNTER IS FARTHEST-FROM-ZERO
               DISPLAY "LC-FFZ T" ELSE DISPLAY "LC-FFZ F" END-IF
           IF LINE-COUNTER IS IN-ARITHMETIC-RANGE
               DISPLAY "LC-IAR T" ELSE DISPLAY "LC-IAR F" END-IF
           TERMINATE RPT
           CLOSE PFILE RFILE
           IF FUNCTION NUMVAL("1.5") IS NUMERIC
               DISPLAY "NV-NUM T" ELSE DISPLAY "NV-NUM F" END-IF
           IF FUNCTION NUMVAL("1.5") IS NOT NUMERIC
               DISPLAY "NV-NOTNUM T" ELSE DISPLAY "NV-NOTNUM F" END-IF
           IF FUNCTION INTEGER(N) IS NUMERIC
               DISPLAY "INT-NUM T" ELSE DISPLAY "INT-NUM F" END-IF
           IF FUNCTION SQRT(4) IS NUMERIC
               DISPLAY "SQ-NUM T" ELSE DISPLAY "SQ-NUM F" END-IF
           IF FUNCTION UPPER-CASE("12") IS NUMERIC
               DISPLAY "UC-NUM T" ELSE DISPLAY "UC-NUM F" END-IF
           IF FUNCTION ABS(-1) IS NEAREST-TO-ZERO
               DISPLAY "ABS-NTZ T" ELSE DISPLAY "ABS-NTZ F" END-IF
           IF FUNCTION INTEGER(N) IS NEAREST-TO-ZERO
               DISPLAY "INT-NTZ T" ELSE DISPLAY "INT-NTZ F" END-IF
           IF FUNCTION SQRT(4) IS NEAREST-TO-ZERO
               DISPLAY "SQ-NTZ T" ELSE DISPLAY "SQ-NTZ F" END-IF
           IF FUNCTION SQRT(4) IS FARTHEST-FROM-ZERO
               DISPLAY "SQ-FFZ T" ELSE DISPLAY "SQ-FFZ F" END-IF
           IF FUNCTION INTEGER(3.5) IS IN-ARITHMETIC-RANGE
               DISPLAY "INT-IAR T" ELSE DISPLAY "INT-IAR F" END-IF
           IF FUNCTION NUMVAL("1.5") IS IN-ARITHMETIC-RANGE
               DISPLAY "NV-IAR T" ELSE DISPLAY "NV-IAR F" END-IF
           COMPUTE R1 = FUNCTION RANDOM(7)
           COMPUTE RX = FUNCTION RANDOM
           COMPUTE R3 = FUNCTION RANDOM
           COMPUTE RX = FUNCTION RANDOM(7)
           IF FUNCTION RANDOM IS NUMERIC
               DISPLAY "RND-NUM T" ELSE DISPLAY "RND-NUM F" END-IF
           COMPUTE RX = FUNCTION RANDOM
           IF RX = R3 DISPLAY "RND-SEQ T" ELSE DISPLAY "RND-SEQ F" END-IF
           STOP RUN.
