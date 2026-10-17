*>--- conformance-case
*> id: pb145_round_prohib
*> rules: [GR-8.8.1.5.2-1, GR-8.8.1.5.2-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      >>TURN EC-SIZE-TRUNCATION EC-SIZE-UNDERFLOW CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB145RP.
      *> kb/Work PB145 - INTERMEDIATE ROUNDING IS PROHIBITED: an inexact
      *> 34-digit reduction raises 11.9.11.2 r3d's EC-SIZE-TRUNCATION,
      *> while a BELOW-RANGE landing is 8.8.1.5.2 r2's too-small condition
      *> and keeps its own name EC-SIZE-UNDERFLOW (the old Clamp path
      *> named it TRUNCATION - two names for one physical condition).
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL
           INTERMEDIATE ROUNDING IS PROHIBITED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9.
       01 Q PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = 2.000000000000000000000000000000005E+33 / 2
               - 1.0E+33
               ON SIZE ERROR DISPLAY "TIE=" FUNCTION EXCEPTION-STATUS
           END-COMPUTE
           COMPUTE Q = (1.5E-6000 / 1.0E+176) * 1.0E+6144 * 1.0E+32
               ON SIZE ERROR DISPLAY "SUB=" FUNCTION EXCEPTION-STATUS
           END-COMPUTE
           STOP RUN.
