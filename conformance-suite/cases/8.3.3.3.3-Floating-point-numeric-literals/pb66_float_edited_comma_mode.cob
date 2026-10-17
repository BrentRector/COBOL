*>--- conformance-case
*> id: pb66_float_edited_comma_mode
*> rules: [SR-8.3.3.3.3-1, SR-13.18.40.3-13, GR-14.6.8.4-1,
*>    GR-14.6.8.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB66 / PB98 - DECIMAL-POINT IS COMMA (12.3.7.4 GR14 a) exchanges the functions of the comma and the period in
      *> PICTURE character-strings (13.18.40.3) AND in numeric literals - the floating-point literal's significand
      *> included (8.3.3.3.3: "two fixed-point numeric literals separated by the letter E"), so `1,5E+3` is ONE
      *> literal of value 1500 (PB98: it was a parse error in the procedure division and silently seeded 1 in a
      *> VALUE clause). A floating-point numeric-edited picture written -9,9(5)E+99 has its decimal point at the
      *> comma; 9.999E+99 has the period as a simple-insertion (grouping) character and a four-digit integer
      *> significand.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB66FC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 PIC -9,9(5)E+99.
       01 E2 PIC +9(3),9(2)E+999 VALUE 1,5E+3.
       01 E3 PIC 9.999E+99.
       01 N1 PIC S9(5)V9(3) VALUE -12345,678.
       01 N2 PIC S9(5)V9(3).
       01 F1 USAGE FLOAT-LONG VALUE 1,5E+3.
       01 F2 USAGE FLOAT-LONG.
       01 U1 PIC 9(5)V99 VALUE 1,5E+3.
       PROCEDURE DIVISION.
           DISPLAY "E2 VALUE [" E2 "]".
           MOVE N1 TO E1
           DISPLAY "N1->E1   [" E1 "]".
           MOVE E1 TO N2
           DISPLAY "E1->N2   [" N2 "]".
           MOVE 1234,5 TO E3
           DISPLAY "->E3     [" E3 "]".
           DISPLAY "F1=" F1 " U1=" U1.
           MOVE 2,5E+2 TO F2
           DISPLAY "F2=" F2.
           MOVE -1,5E-1 TO N2
           DISPLAY "N2=" N2.
           COMPUTE N2 = 1,25E+2 + 0,75E+2
           DISPLAY "N2=" N2.
           IF FUNCTION ABS(-2,5E+1) = 25 DISPLAY "FUNCTION ARG OK" END-IF
           STOP RUN.
