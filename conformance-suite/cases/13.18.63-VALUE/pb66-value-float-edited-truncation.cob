*>--- conformance-case
*> id: pb66-value-float-edited-truncation
*> rules: [SR-13.18.63.3-3, SR-13.18.63.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 13.18.63.3 SR6 for the floating-point numeric-edited item (D21/PB66): the floating-point
      *> literal converts per the MOVE rules "such that no truncation of digits or sign is required" - 1.234E+5
      *> has four significant digits for a three-digit significand, 1.0E+10 lies beyond a one-digit exponent
      *> (+9), 1.23E-10 below it, and -1.23E+5 has no sign position in 9.99E+9 (SR3): COBOLNET1625 for each.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB66NVT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FE1 PIC 9.99E+9 VALUE 1.234E+5.
       01 FE2 PIC 9.99E+9 VALUE 1.0E+10.
       01 FE3 PIC 9.99E+9 VALUE 1.23E-10.
       01 FE4 PIC 9.99E+9 VALUE -1.23E+5.
       PROCEDURE DIVISION.
           STOP RUN.
