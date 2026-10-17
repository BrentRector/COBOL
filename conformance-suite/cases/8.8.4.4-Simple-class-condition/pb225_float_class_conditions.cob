*>--- conformance-case
*> id: pb225_float_class_conditions
*> rules: [SR-8.8.4.4.3-6, SR-8.8.4.4.3-7, GR-8.8.4.4.4-3,
*>    FMT-8.8.4.4.2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 8.8.4.4.2 - the seven COBOL-2014 numeric-content alternatives of the class condition,
      *> each over the operand SR6/SR7 admit (kb/Work PB225). Every expected value is the rule's, not a run's:
      *>   8.8.4.4.4 GR3 h) FLOAT-INFINITY      - positive or negative infinity (ISO/IEC 60559 clause 3)
      *>                 i) FLOAT-NOT-A-NUMBER  - a quiet OR a signaling NaN
      *>                 j) ...-QUIET / k) ...-SIGNALING - the leading significand bit set / clear (60559 6.2.1)
      *>                 g) FARTHEST-FROM-ZERO  - the value farthest from zero the item may contain, either sign
      *>                 m) NEAREST-TO-ZERO     - the nonzero value nearest to zero, either sign
      *>                 l) IN-ARITHMETIC-RANGE - within the intermediate's range (a finite value, here: binary64
      *>                                          is the widest carrier and the intermediate contains it)
      *>                 n) 1. b. NUMERIC over a STANDARD float usage - "true only if ... a finite numeric value"
      *> The values are stored by SET CONTENT (14.9.39 format 15 - the same extremes, Annex D.32) and, for the
      *> binary32 carrier, as raw big-endian IEEE bytes through a REDEFINES window (13.18.60.4 GR19 default
      *> order), so the class test reads the image's own bits. Column letters on each D line, in order:
      *> INF NAN QNAN SNAN FAR NEAR IAR NUM.
      *> DETERMINATION (docs/CONFORMANCE.md is not needed - it is a reading of GR3 g), not an implementor choice):
      *> "whether that value is positive or negative" admits EITHER direction's extreme, so a PIC S9(4) COMP-5
      *> item (13.18.60.4 GR12: -32768..32767) is FARTHEST-FROM-ZERO at both ends (C5-NEG / C5-POS).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB225FLOATCLASS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D   USAGE FLOAT-BINARY-64.
       01 W4  PIC X(4).
       01 S   REDEFINES W4 USAGE FLOAT-BINARY-32.
       01 N   PIC S9(3)V99.
       01 U   PIC 9(2).
       01 C5  PIC S9(4) COMP-5.
       01 R   PIC X(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1.5 TO D
           PERFORM SHOW-D
           SET CONTENT OF D TO FLOAT-INFINITY SIGN NEGATIVE
           PERFORM SHOW-D
           SET CONTENT OF D TO FLOAT-NOT-A-NUMBER
           PERFORM SHOW-D
           SET CONTENT OF D TO FLOAT-NOT-A-NUMBER-SIGNALING
           PERFORM SHOW-D
           SET CONTENT OF D TO FARTHEST-FROM-ZERO SIGN NEGATIVE
           PERFORM SHOW-D
           SET CONTENT OF D TO NEAREST-TO-ZERO
           PERFORM SHOW-D
           MOVE 0 TO D
           PERFORM SHOW-D
      *> binary32 through the window: signaling NaN, quiet NaN, -infinity, largest finite, least subnormal.
           MOVE X"7F800001" TO W4
           PERFORM SHOW-S
           MOVE X"7FC00000" TO W4
           PERFORM SHOW-S
           MOVE X"FF800000" TO W4
           PERFORM SHOW-S
           MOVE X"7F7FFFFF" TO W4
           PERFORM SHOW-S
           MOVE X"80000001" TO W4
           PERFORM SHOW-S
      *> Fixed point: the PICTURE's own extremes (S9(3)V99: +-999.99, nearest +-0.01).
           MOVE -999.99 TO N
           IF N IS FARTHEST-FROM-ZERO DISPLAY "N-FAR-NEG=Y" ELSE DISPLAY "N-FAR-NEG=N" END-IF
           MOVE 999.99 TO N
           IF N IS FARTHEST-FROM-ZERO DISPLAY "N-FAR-POS=Y" ELSE DISPLAY "N-FAR-POS=N" END-IF
           MOVE 999.98 TO N
           IF N IS NOT FARTHEST-FROM-ZERO DISPLAY "N-NOT-FAR=Y" ELSE DISPLAY "N-NOT-FAR=N" END-IF
           MOVE -0.01 TO N
           IF N IS NEAREST-TO-ZERO DISPLAY "N-NEAR-NEG=Y" ELSE DISPLAY "N-NEAR-NEG=N" END-IF
           MOVE 0 TO N
           IF N IS NEAREST-TO-ZERO DISPLAY "N-NEAR-ZERO=Y" ELSE DISPLAY "N-NEAR-ZERO=N" END-IF
           IF N IS IN-ARITHMETIC-RANGE DISPLAY "N-IAR=Y" ELSE DISPLAY "N-IAR=N" END-IF
           MOVE 99 TO U
           IF U IS FARTHEST-FROM-ZERO DISPLAY "U-FAR=Y" ELSE DISPLAY "U-FAR=N" END-IF
           MOVE 1 TO U
           IF U IS NEAREST-TO-ZERO DISPLAY "U-NEAR=Y" ELSE DISPLAY "U-NEAR=N" END-IF
      *> A two's-complement container (13.18.60.4 GR12): both ends are extremes; 9999 (the PICTURE's) is not.
           SET CONTENT OF C5 TO FARTHEST-FROM-ZERO SIGN NEGATIVE
           IF C5 IS FARTHEST-FROM-ZERO DISPLAY "C5-NEG=Y" ELSE DISPLAY "C5-NEG=N" END-IF
           SET CONTENT OF C5 TO FARTHEST-FROM-ZERO SIGN POSITIVE
           IF C5 IS FARTHEST-FROM-ZERO DISPLAY "C5-POS=Y" ELSE DISPLAY "C5-POS=N" END-IF
           MOVE 9999 TO C5
           IF C5 IS FARTHEST-FROM-ZERO DISPLAY "C5-9999=Y" ELSE DISPLAY "C5-9999=N" END-IF
           STOP RUN.
       SHOW-D.
           MOVE "NNNNNNNN" TO R
           IF D IS FLOAT-INFINITY MOVE "Y" TO R (1:1) END-IF
           IF D IS FLOAT-NOT-A-NUMBER MOVE "Y" TO R (2:1) END-IF
           IF D IS FLOAT-NOT-A-NUMBER-QUIET MOVE "Y" TO R (3:1) END-IF
           IF D IS FLOAT-NOT-A-NUMBER-SIGNALING MOVE "Y" TO R (4:1) END-IF
           IF D IS FARTHEST-FROM-ZERO MOVE "Y" TO R (5:1) END-IF
           IF D IS NEAREST-TO-ZERO MOVE "Y" TO R (6:1) END-IF
           IF D IS IN-ARITHMETIC-RANGE MOVE "Y" TO R (7:1) END-IF
           IF D IS NUMERIC MOVE "Y" TO R (8:1) END-IF
           DISPLAY "D=" R.
       SHOW-S.
           MOVE "NNNNNNNN" TO R
           IF S IS FLOAT-INFINITY MOVE "Y" TO R (1:1) END-IF
           IF S IS FLOAT-NOT-A-NUMBER MOVE "Y" TO R (2:1) END-IF
           IF S IS FLOAT-NOT-A-NUMBER-QUIET MOVE "Y" TO R (3:1) END-IF
           IF S IS NOT FLOAT-NOT-A-NUMBER-SIGNALING
               CONTINUE
           ELSE
               MOVE "Y" TO R (4:1)
           END-IF
           IF S IS FARTHEST-FROM-ZERO MOVE "Y" TO R (5:1) END-IF
           IF S IS NEAREST-TO-ZERO MOVE "Y" TO R (6:1) END-IF
           IF S IS IN-ARITHMETIC-RANGE MOVE "Y" TO R (7:1) END-IF
           IF S IS NUMERIC MOVE "Y" TO R (8:1) END-IF
           DISPLAY "S=" R.
