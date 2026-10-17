*>--- conformance-case
*> id: pb647_float_landing_mode_2002
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB647 at COBOL-2002 — ONE returned value, ONE rounding decision, taken at the RESULTANT
      *> IDENTIFIER. The 2014/2023 twins add the eight-mode ROUNDED MODE IS phrase and the OPTIONS
      *> DEFAULT ROUNDED MODE clause, neither of which exists at this edition; what IS testable here is the
      *> whole substance of the defect, because it needed no MODE phrase to reach a program: the
      *> float-intrinsic quantizer rounded NEAREST-AWAY-FROM-ZERO at a working scale regardless, so
      *> `MOVE FUNCTION SQRT(3) TO R9` gave 1.732050807 and `COMPUTE R9 = FUNCTION SQRT(3)` gave
      *> 1.732050808 into ONE PIC 9V9(9), which ISO 15.4.1 forbids -- "the returned value is the same for
      *> all instances of a given function within a single execution of the runtime element so long as the
      *> value and order of the arguments, the collating sequence, and the locale are unchanged".
      *>
      *> 14.6.8.2 rule 4 gives the MOVE its landing: "If the receiving operand is a fixed-point numeric item,
      *> the data is aligned by decimal point and is transferred to the receiving digits with zero fill or
      *> truncation on either end as required."  14.7.4.1 gives the arithmetic store the same shape: "If,
      *> after decimal point alignment, the number of places in the fractional part of the result of an
      *> arithmetic operation is greater than the number of places provided for the fraction of the resultant
      *> identifier, truncation is relative to the size provided for the resultant identifier."  14.7.4.3
      *> rule 2 -- "If the ROUNDED phrase is not specified, execution is as if ROUNDED MODE IS TRUNCATION had
      *> been specified" -- and rule 4 makes a bare ROUNDED the nearest value, ties farther from zero.
      *>
      *> EVERY EXPECTED VALUE IS THE EXACT DECIMAL EXPANSION OF THE BINARY64, COMPUTED, NEVER OBSERVED:
      *>   sqrt(3)            = 1.732050807568877193176604123436845839023590087890625
      *>       scale 9 -> the discarded tail begins with 5: TRUNCATION 1.732050807, ROUNDED 1.732050808.
      *>       scale 4 -> 1.7320 truncated, 1.7321 rounded.
      *>   sqrt(0.9999999999) = 0.99999999994999999586298145004548132419586181640625
      *>       scale 1 -> 0.9 truncated, 1.0 rounded: ONE returned value split across a whole tenth.
      *> 3 ** 0.5 is the SAME binary64 as FUNCTION SQRT(3) and takes the SAME landing -- the native
      *> exponentiation quantizer is the float family's, and it carried the identical hard-coded mode.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB647FM2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R9  PIC 9V9(9).
       01 R1  PIC 9V9.
       01 R4  PIC 9V9(4).
       PROCEDURE DIVISION.
       MAIN.
      *> A -- the defect: one returned value, two channels, one receiver.
           MOVE FUNCTION SQRT(3) TO R9
           DISPLAY "A1=" R9
           COMPUTE R9 = FUNCTION SQRT(3)
           DISPLAY "A2=" R9
      *> B -- the bare ROUNDED phrase now reaches the family at all.
           COMPUTE R9 ROUNDED = FUNCTION SQRT(3)
           DISPLAY "B1=" R9
      *> D -- the split across a whole tenth, both channels.
           MOVE FUNCTION SQRT(0.9999999999) TO R1
           DISPLAY "D1=" R1
           COMPUTE R1 = FUNCTION SQRT(0.9999999999)
           DISPLAY "D2=" R1
           COMPUTE R1 ROUNDED = FUNCTION SQRT(0.9999999999)
           DISPLAY "D3=" R1
      *> E -- native ** is the same quantizer and takes the same landing.
           COMPUTE R9 = 3 ** 0.5
           DISPLAY "E1=" R9
           COMPUTE R9 ROUNDED = 3 ** 0.5
           DISPLAY "E2=" R9
      *> G -- a NESTED float operand is not the final transfer: it lands truncated at the working scale and
      *> the ONE receiver store performs the statement's rounding (14.7.4.3 rules 3-10 each name "the
      *> resultant identifier"). At scale 4 the working scale still carries five spare digits, so both
      *> answers are the exact expansion's, rounded once.
           COMPUTE R4 = FUNCTION SQRT(3) * 1
           DISPLAY "G1=" R4
           COMPUTE R4 ROUNDED = FUNCTION SQRT(3) * 1
           DISPLAY "G2=" R4
           STOP RUN.
