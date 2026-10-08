      *> kb/Work PB2163 sibling sweep - an UNSIGNED 16-byte COMP-5 item (the UInt128 carrier, kb/Work
      *> R10) stored into EVERY numeric-edited receiver form: a masked picture, a format-2 LOCALE
      *> picture and a floating-point edited picture, by COMPUTE (with and without ON SIZE ERROR) and
      *> by MOVE. Each of the six forms failed to compile (CS1503: the UInt128 was handed to the
      *> Int128 edit kernels) until the edited landing became one carrier switch.
      *> Expected values from the standard:
      *>   14.7.7 1) "any necessary conversion and decimal point alignment is supplied throughout
      *>   the calculation"; 14.9.25.4 6) "Alignment of the numeric value by decimal point, any
      *>   necessary zero filling, any truncation of digits" - so 12345 edits as 12345.00 and the
      *>   container maximum 2**128-1 = 340282366920938463463374607431768211455 MOVEs as its low-order
      *>   29 integer digits, 20938463463374607431768211455.00 (a MOVE carries the full container
      *>   range, CONFORMANCE.md DOC-A.1-179).
      *>   14.7.5 3) "further from zero than permitted for the associated resultant data item" - the
      *>   size error: 12345 into ZZZ9, and 2**128-1 into Z(28)9.99, leave the receiver unchanged.
      *>   14.7.4.3 7) "If the PROHIBITED phrase is specified, and the arithmetic value cannot be
      *>   represented exactly in the resultant identifier" - 123.55 into ZZZ9 is a size error.
      *>   ROUNDED (NEAREST-AWAY-FROM-ZERO) lands 123.55 as 124; no ROUNDED truncates it to 123.
      *>   The floating-point edited form keeps 5 significant digits, truncated: +3.4028E+38.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2163UW.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U  PIC 9(31) COMP-5 VALUE 12345.
       01 UV PIC 9(29)V99 COMP-5 VALUE 123.55.
       01 E  PIC Z(28)9.99.
       01 E2 PIC ZZZ9 VALUE 9.
       01 L  PIC ZZZZZZ9.99 LOCALE IS US SIZE IS 12.
       01 F  PIC +9.9999E+99.
       PROCEDURE DIVISION.
           COMPUTE E = U
           DISPLAY "C-E  [" E "]"
           COMPUTE E2 = U ON SIZE ERROR DISPLAY "C-E2 SIZE" END-COMPUTE
           DISPLAY "C-E2 [" E2 "]"
           COMPUTE L = U
           DISPLAY "C-L  [" L "]"
           MOVE 0 TO L
           COMPUTE L = U ON SIZE ERROR DISPLAY "C-L SIZE" END-COMPUTE
           DISPLAY "C-L  [" L "]"
           COMPUTE F = U
           DISPLAY "C-F  [" F "]"
           COMPUTE F = U ON SIZE ERROR DISPLAY "C-F SIZE" END-COMPUTE
           DISPLAY "C-F  [" F "]"
           COMPUTE E2 = UV
           DISPLAY "T-E2 [" E2 "]"
           COMPUTE E2 ROUNDED = UV
           DISPLAY "R-E2 [" E2 "]"
           COMPUTE E2 ROUNDED MODE IS PROHIBITED = UV
               ON SIZE ERROR DISPLAY "P-E2 SIZE"
           END-COMPUTE
           DISPLAY "P-E2 [" E2 "]"
           MOVE U TO E
           DISPLAY "M-E  [" E "]"
           MOVE U TO L
           DISPLAY "M-L  [" L "]"
           MOVE U TO F
           DISPLAY "M-F  [" F "]"
           MOVE FUNCTION HIGHEST-ALGEBRAIC(U) TO U
           MOVE U TO E
           DISPLAY "X-E  [" E "]"
           MOVE U TO F
           DISPLAY "X-F  [" F "]"
           MOVE 0 TO E
           COMPUTE E = U ON SIZE ERROR DISPLAY "Y-E SIZE" END-COMPUTE
           DISPLAY "Y-E  [" E "]"
           COMPUTE E = U
           DISPLAY "Z-E  [" E "]"
           STOP RUN.
