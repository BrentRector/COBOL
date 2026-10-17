*>--- conformance-case
*> id: pb2918_native_dec_lane_wide_binary_operand
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2918 - the NATIVE statement whose expression carries a
      *>   floating-point LITERAL evaluates on the standard-decimal
      *>   intermediate (CONFORMANCE.md DOC-A.1-82: the lane is the
      *>   implementor's determination under 8.8.1.3, 34 significant
      *>   digits), and a 16-byte binary container operand enters that
      *>   lane the way 8.8.1.5.2 r1 gives every operand: rounded to the
      *>   SDIDI's 34 digits under 11.9.11 when it cannot be expressed
      *>   exactly (cite.py --check 8.8.1.5.2 "If the operand value cannot
      *>   be expressed exactly in an SDIDI, the value is rounded
      *>   according to the rules in 11.9.11" -> OK 8.8.1.5.2 1)).
      *>
      *>   S PIC S9(19) COMP-5 = -2**127 =
      *>   -170141183460469231731687303715884105728 (39 digits).
      *>   S + 0.5E0: S enters as -170141183460469231731687303715884100000,
      *>   the sum -...099999.5 rounds away to the same 34-digit value, and
      *>   R (container -2**127 .. 2**127-1) holds it: stored, low 19 digits
      *>   -1687303715884100000.  The unrounded lift reported a size error.
      *>   S + S * 0.5E0 = 1.5 * S = -2.55E+38 is outside every
      *>   container: 14.7.5 case 3, size error, R unchanged.  (Before the
      *>   fix the unrounded significand wrapped and a stored value came
      *>   back with NOT ON SIZE ERROR.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2918N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 S PIC S9(19) COMP-5.
       01 GX REDEFINES G PIC X(16).
       01 R PIC S9(19) COMP-5 VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           MOVE X"80000000000000000000000000000000" TO GX.
           COMPUTE R = S + 0.5E0
               ON SIZE ERROR DISPLAY "S+0.5 size error"
               NOT ON SIZE ERROR DISPLAY "S+0.5 R " R
           END-COMPUTE.
           MOVE 7 TO R.
           COMPUTE R = S + S * 0.5E0
               ON SIZE ERROR DISPLAY "1.5S size error"
               NOT ON SIZE ERROR DISPLAY "1.5S R " R
           END-COMPUTE.
           DISPLAY "R " R.
           STOP RUN.
