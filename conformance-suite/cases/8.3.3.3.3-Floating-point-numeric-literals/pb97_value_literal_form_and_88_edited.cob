*>--- conformance-case
*> id: pb97_value_literal_form_and_88_edited
*> rules: [GR-8.3.3.3.3-5, SR-13.18.63.3-3, SR-13.18.63.3-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB97 - the numeric VALUE literal's FORM and the numeric-edited condition-name. (1) A floating-point
      *> literal is a numeric literal (8.3.3.3.3) whose value is significand x 10^exponent (GR5): on a
      *> fixed-point numeric subject it is legal when that value is representable exactly (13.18.63.3 SR2) - it
      *> used to crash the backend (CS0595). (2) A numeric-edited item's numeric VALUE literals convert "according
      *> to the rules for the MOVE statement" in formats 1, 2 AND 4 (13.18.63.3 SR6), so a level-88 on a
      *> numeric-edited conditional variable compares the EDITED image (8.8.4.5 GR2 - the relation-condition
      *> rules) - the raw text "10" was compared to " 10.00" and every such condition-name was silently false.
      *> (3) The floating-point numeric-edited item's VALUE (D21/PB66): a floating-point literal, ZERO, or the
      *> integer / decimal literal zero, each edited as MOVE would.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB97VL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NU1 PIC 9(5)V99 VALUE 1.5E+3.
       01 NU2 PIC S9(5)V99 VALUE -1.5E+3.
       01 NU3 PIC 9(5)V99 VALUE 0.0E+0.
       01 NU4 PIC 9V9(8) VALUE 1.234E-5.
       01 NU5 PIC 9(3) VALUE 12.5E+1.
       01 NU6 COMP-5 PIC S9(4) VALUE 1.5E+3.
       01 NE PIC ZZ9.99.
          88 NE-IS-TEN VALUE 10.
          88 NE-IS-ZERO VALUE ZERO.
          88 NE-SMALL VALUE 1 THRU 5.
          88 NE-LITERAL VALUE " 10.00".
       01 FE PIC +9.9(3)E+99.
          88 FE-IS-BIG VALUE 1.5E+3.
          88 FE-IS-ZERO VALUE ZERO.
          88 FE-RANGE VALUE 1.0E+2 THRU 2.0E+2.
       01 NU PIC 9(5)V99.
          88 NU-IS-BIG VALUE 1.5E+3.
       01 F5 PIC +9.9(3)E+99 VALUE 1.5E+3.
       01 F6 PIC +9.9(3)E+99 VALUE ZERO.
       01 F7 PIC -9.9(3)E+99 VALUE 0.
       01 F8 PIC -9.9(3)E+99 VALUE 0.00.
       01 F9 PIC 9.9(3)E+99 BLANK WHEN ZERO VALUE ZERO.
       PROCEDURE DIVISION.
           DISPLAY "NU1=" NU1 " NU2=" NU2 " NU3=" NU3 " NU4=" NU4 " NU5=" NU5 " NU6=" NU6.
           MOVE 10 TO NE
           IF NE-IS-TEN DISPLAY "NE-IS-TEN true [" NE "]" END-IF
           IF NE-LITERAL DISPLAY "NE-LITERAL true" END-IF
           IF NOT NE-IS-ZERO DISPLAY "NE-IS-ZERO false" END-IF
           MOVE 0 TO NE
           IF NE-IS-ZERO DISPLAY "NE-IS-ZERO true [" NE "]" END-IF
           MOVE 3 TO NE
           IF NE-SMALL DISPLAY "NE-SMALL true [" NE "]" END-IF
           MOVE 6 TO NE
           IF NOT NE-SMALL DISPLAY "NE-SMALL false [" NE "]" END-IF
           MOVE 1500 TO FE
           IF FE-IS-BIG DISPLAY "FE-IS-BIG true [" FE "]" END-IF
           MOVE 0 TO FE
           IF FE-IS-ZERO DISPLAY "FE-IS-ZERO true [" FE "]" END-IF
           MOVE 150 TO FE
           IF FE-RANGE DISPLAY "FE-RANGE true [" FE "]" END-IF
           MOVE 1500 TO NU
           IF NU-IS-BIG DISPLAY "NU-IS-BIG true" END-IF
           DISPLAY "F5=[" F5 "] F6=[" F6 "] F7=[" F7 "] F8=[" F8 "] F9=[" F9 "]".
           STOP RUN.
