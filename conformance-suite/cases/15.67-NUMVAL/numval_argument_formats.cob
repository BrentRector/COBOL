*>--- conformance-case
*> id: numval_argument_formats
*> rules: [FMT-15.67.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.67 NUMVAL — the general format (§15.67.2): FUNCTION NUMVAL ( argument-1 ), across the
      *> §15.67.3 r1 argument shapes: the leading-sign form and the trailing-CR form.
      *> §15.67.3 r2: "Leading and trailing spaces in argument-1 are ignored. Embedded spaces in
      *> argument-1 are ignored only if they appear before the first digit" — V2's "+  12" is legal.
      *> §15.67.4 r1: the returned value is the numeric value represented by argument-1.
      *> §15.67.4 r2: "If argument-1 contains CR, DB, or the minus sign, the returned value is negative."
      *> Receivers are numeric-edited, so every output character derives from the PICTURE clause's §13.18.40.5
      *> editing
      *> rules: 12.34 in 99.99 → "12.34"; 12 → "12.00"; -12.5 in -99.9 → "-12.5"; -0.5 → "-00.5".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NUMVALFMT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1  PIC 99.99.
       01 E2  PIC -99.9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION NUMVAL(" 12.34 ") TO E1
           DISPLAY "V1=" E1
           MOVE FUNCTION NUMVAL("+  12") TO E1
           DISPLAY "V2=" E1
           MOVE FUNCTION NUMVAL("12.5CR") TO E2
           DISPLAY "V3=" E2
           MOVE FUNCTION NUMVAL("  -.5") TO E2
           DISPLAY "V4=" E2
           STOP RUN.
