*>--- conformance-case
*> id: pb66_float_edited_picture
*> rules: [SR-13.18.40.3-23, GR-13.18.40.4-13, GR-13.18.40.5-8, SR-13.18.63.3-6, GR-14.6.8.4-1, GR-14.6.8.4-2, GR-14.7.5-3, GR-14.7.5-4, SR-14.9.2.3-4, GR-14.9.2.4-2, GR-14.9.25.4-5, GR-14.9.25.4-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB66 - the FLOATING-POINT NUMERIC-EDITED picture (13.18.40.4 GR13 b: a significand character-string and
      *> an exponent +9{1..4} joined by the symbol E; category numeric-edited, class alphanumeric). A store
      *> normalizes the value so the significand's leading digit is nonzero (14.6.8.4 GR1), aligns and truncates
      *> per 13.18.40 (GR2); a zero value zeroes every digit position with positive signs (13.18.40.5 r8);
      *> a value farther from zero than the picture permits is EC-DATA-OVERFLOW (unchecked here: the pinned
      *> saturated image, CONFORMANCE.md), one nearer to zero is treated as zero (14.9.25.4 GR6 item 4); the
      *> item de-edits as a numeric-edited sender (14.9.25.4 GR5 - the algebraic value); an arithmetic
      *> statement's floating-point edited resultant takes the size error condition in BOTH directions out of
      *> range (14.7.5 cases 3 and 4 - the receiver unchanged), else the MOVE disposition. Every expected image
      *> is derived from the rules, not observed: -12345.678 into -9.9(5)E+99 is significand 123456 (the six
      *> digits after truncation) at exponent 4 -> -1.23456E+04.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB66FE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 PIC -9.9(5)E+99.
       01 E2 PIC +9(3).9(2)E+999.
       01 E3 PIC 9.99E+9.
       01 E4 PIC -9(2).9(3)E+99.
       01 E5 PIC +9.9(3)E+99 VALUE 1.5E+3.
       01 E6 PIC +9.9(3)E+99 VALUE ZERO.
       01 E7 PIC -9.9(3)E+99 VALUE 0.
       01 E8 PIC 9.9(3)E+99 BLANK WHEN ZERO.
       01 E9 PIC 9(3)E+99.
       01 N1 PIC S9(5)V9(3) VALUE -12345.678.
       01 N2 PIC S9(5)V9(3).
       01 F1 USAGE FLOAT-LONG VALUE 6.02214076E+23.
       01 X1 PIC X(12).
       01 NE PIC -ZZ,ZZ9.99.
       PROCEDURE DIVISION.
           DISPLAY "VALUE E5=[" E5 "] E6=[" E6 "] E7=[" E7 "]".
           MOVE N1 TO E1
           DISPLAY "N1->E1 [" E1 "]".
           MOVE N1 TO E2
           DISPLAY "N1->E2 [" E2 "]".
           MOVE 0 TO E1
           DISPLAY "0->E1  [" E1 "]".
           MOVE ZERO TO E2
           DISPLAY "0->E2  [" E2 "]".
           MOVE 0 TO E8
           DISPLAY "0->E8  [" E8 "] (BLANK WHEN ZERO)".
           MOVE 0.00012345 TO E3
           DISPLAY "->E3   [" E3 "]".
           MOVE 999 TO E3
           DISPLAY "999->E3[" E3 "]".
           MOVE 1.0E+10 TO E3
           DISPLAY "1E+10  [" E3 "] (overflow, unchecked: the pinned image)".
           MOVE 1.0E-12 TO E3
           DISPLAY "1E-12  [" E3 "] (underflow: zero)".
           MOVE F1 TO E1
           DISPLAY "F1->E1 [" E1 "]".
           MOVE -5 TO E4
           DISPLAY "-5->E4 [" E4 "]".
           MOVE 1234 TO E9
           DISPLAY "1234->E9 [" E9 "]".
           MOVE N1 TO E1
           MOVE E1 TO N2
           DISPLAY "E1->N2 [" N2 "]".
           MOVE E1 TO X1
           DISPLAY "E1->X1 [" X1 "]".
           MOVE E1 TO E2
           DISPLAY "E1->E2 [" E2 "]".
           MOVE E1 TO NE
           DISPLAY "E1->NE [" NE "]".
           MOVE E4 TO N2
           DISPLAY "E4->N2 [" N2 "]".
           COMPUTE E3 = 999 * 100
           DISPLAY "COMPUTE [" E3 "]".
           COMPUTE E3 = 1.0E+12
               ON SIZE ERROR DISPLAY "SIZE ERROR (overflow)"
           END-COMPUTE
           DISPLAY "after   [" E3 "] unchanged".
           COMPUTE E3 = 1.0E-12
               ON SIZE ERROR DISPLAY "SIZE ERROR (underflow)"
           END-COMPUTE
           DISPLAY "after   [" E3 "] unchanged".
           COMPUTE E3 = 1.0E+12
           DISPLAY "no phrase [" E3 "] (the MOVE disposition: pinned)".
           MULTIPLY 2 BY N1 GIVING E2
           DISPLAY "MULTIPLY [" E2 "]".
           DIVIDE 3 INTO 1 GIVING E1
           DISPLAY "DIVIDE   [" E1 "]".
           ADD 1 0.5 GIVING E4
           DISPLAY "ADD      [" E4 "]".
           STOP RUN.
