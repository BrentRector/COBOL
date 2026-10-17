*>--- conformance-case
*> id: pb167_sdidi_exponentiation_prohibited
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB167 - the INTERMEDIATE ROUNDING IS PROHIBITED consequence of owner decision D-C, pinned
      *> because it is a deliberate BEHAVIOUR CHANGE and the standard requires it.
      *>
      *> 11.9.11.2 rule 3 d: "If the PROHIBITED phrase is specified and an intermediate value cannot be
      *> represented exactly in SDIDI form, the EC-SIZE-TRUNCATION exception condition is set to exist and
      *> the results of the operation are undefined."
      *>
      *> The 8.8.1.5.4 r2e development of a non-integer power is a chain of inexact 34-digit operations over
      *> an irrational result, so under PROHIBITED it raises - which is exactly what r3d asks for and exactly
      *> what FUNCTION SQRT already did for an inexact root.  The former binary64 core did NOT raise: it
      *> computed in IEEE double and entered the SDIDI through ONE exact FromDouble conversion, so the
      *> prohibition never saw the inexactness it exists to report.  An INTEGER power still computes: its
      *> r2a-r2d development is exact multiplication and nothing rounds.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB167PRH.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL
           INTERMEDIATE ROUNDING IS PROHIBITED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(4)V9(9).
       PROCEDURE DIVISION.
           COMPUTE A = 2 ** 0.25
             ON SIZE ERROR DISPLAY "NONINT=SIZE ERROR"
             NOT ON SIZE ERROR DISPLAY "NONINT=" A
           END-COMPUTE.
           COMPUTE A = 2 ** 0.5
             ON SIZE ERROR DISPLAY "HALF  =SIZE ERROR"
             NOT ON SIZE ERROR DISPLAY "HALF  =" A
           END-COMPUTE.
           COMPUTE A = 2 ** 10
             ON SIZE ERROR DISPLAY "INT   =SIZE ERROR"
             NOT ON SIZE ERROR DISPLAY "INT   =" A
           END-COMPUTE.
           STOP RUN.
