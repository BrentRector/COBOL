*>--- conformance-case
*> id: pb74_sdidi_intermediate_prohibited_ec
*> rules: [GR-8.8.1.5.2-3, GR-14.7.5-8]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB74 (sweep) - INTERMEDIATE ROUNDING IS PROHIBITED under STANDARD-DECIMAL: 11.9.11.2 r3d - "If the
      *> PROHIBITED phrase is specified and an intermediate value cannot be represented exactly in SDIDI
      *> form, the EC-SIZE-TRUNCATION exception condition is set to exist and the results of the operation
      *> are undefined." 2 / 3 is inexact in 34 digits; the raise carried the default EC-SIZE-OVERFLOW name, so
      *> an EXCEPTION-STATUS (or a level-3 USE / PERFORM WHEN selection) saw the wrong condition. Control: an
      *> exact intermediate (2 / 4) takes NOT ON SIZE ERROR.
      >>TURN EC-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB74SDINTPROH.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL
           INTERMEDIATE ROUNDING IS PROHIBITED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC 9V9 VALUE 4.
       PROCEDURE DIVISION.
           COMPUTE WS-X = 2 / 3
               ON SIZE ERROR DISPLAY "INEXACT=" FUNCTION EXCEPTION-STATUS
               NOT ON SIZE ERROR DISPLAY "INEXACT=NOSE"
           END-COMPUTE.
           DISPLAY "X=" WS-X.
           COMPUTE WS-X = 2 / 4
               ON SIZE ERROR DISPLAY "EXACT=" FUNCTION EXCEPTION-STATUS
               NOT ON SIZE ERROR DISPLAY "EXACT=NOSE X=" WS-X
           END-COMPUTE.
           STOP RUN.
       END PROGRAM PB74SDINTPROH.
