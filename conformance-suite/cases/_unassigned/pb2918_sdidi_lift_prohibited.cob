*>--- conformance-case
*> id: pb2918_sdidi_lift_prohibited
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       >>TURN EC-SIZE-TRUNCATION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2918P.
      *> kb/Work PB2918 - the lift of a 39-digit binary container value
      *>   under INTERMEDIATE ROUNDING IS PROHIBITED.
      *>
      *>   8.8.1.5.2 r1 sends an operand the SDIDI cannot hold exactly to
      *>   the rules of 11.9.11, and 11.9.11.2 r3d: "If the PROHIBITED
      *>   phrase is specified and an intermediate value cannot be
      *>   represented exactly in SDIDI form, the EC-SIZE-TRUNCATION
      *>   exception condition is set to exist" (cite.py --check
      *>   11.9.11.2 "If the PROHIBITED phrase is specified and an
      *>   intermediate value cannot be represented exactly in SDIDI
      *>   form, the EC-SIZE-TRUNCATION exception condition is set to
      *>   exist" -> OK 11.9.11.2 3)).
      *>   S = -2**127 has 39 digits: the lift is inexact, so
      *>   COMPUTE R = S + 1 takes ON SIZE ERROR with R unchanged and
      *>   EC-SIZE-TRUNCATION.  S2 = -(10**33) (34 digits, exact) lifts
      *>   with no rounding, so S2 + 1 is stored: PROHIBITED never
      *>   refuses a value the SDIDI holds.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL
           INTERMEDIATE ROUNDING IS PROHIBITED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 S PIC S9(19) COMP-5.
       01 GX REDEFINES G PIC X(16).
       01 H.
          05 S2 PIC S9(19) COMP-5.
       01 HX REDEFINES H PIC X(16).
       01 R PIC S9(19) COMP-5 VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           MOVE X"80000000000000000000000000000000" TO GX.
      *>   X"FFFFCEB2..." is 2**128 - 10**33, i.e. -(10**33).
           MOVE X"FFFFCEB239BB726CC73EA4F600000000" TO HX.
           COMPUTE R = S + 1
               ON SIZE ERROR DISPLAY "S+1 " FUNCTION EXCEPTION-STATUS
               NOT ON SIZE ERROR DISPLAY "S+1 stored " R
           END-COMPUTE.
           DISPLAY "R " R.
           COMPUTE R = S2 + 2
               ON SIZE ERROR DISPLAY "S2+2 size error"
               NOT ON SIZE ERROR DISPLAY "S2+2 R " R
           END-COMPUTE.
           STOP RUN.
