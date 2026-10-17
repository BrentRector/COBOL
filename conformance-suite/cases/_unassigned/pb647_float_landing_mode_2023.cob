*>--- conformance-case
*> id: pb647_float_landing_mode_2023
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB647 — ONE returned value, ONE rounding decision, taken at the RESULTANT IDENTIFIER.
      *> PB623 made the arithmetic channel and the MOVE channel compute the same exact binary64 expansion;
      *> what still split them was the MODE. The float-intrinsic quantizer rounded NEAREST-AWAY-FROM-ZERO at
      *> a working scale no matter what the statement said, so `MOVE FUNCTION SQRT(3) TO R9` gave
      *> 1.732050807 and `COMPUTE R9 = FUNCTION SQRT(3)` gave 1.732050808 into ONE PIC 9V9(9), and the
      *> ROUNDED phrase was a NO-OP on the whole family.
      *>
      *> THE RULES. ISO 15.4.1: "the returned value is the same for all instances of a given function within
      *> a single execution of the runtime element so long as the value and order of the arguments, the
      *> collating sequence, and the locale are unchanged" -- so the two channels may not disagree.
      *> 14.6.8.2 rule 4 gives the MOVE its landing: "If the receiving operand is a fixed-point numeric item,
      *> the data is aligned by decimal point and is transferred to the receiving digits with zero fill or
      *> truncation on either end as required."  14.7.4.1 gives the arithmetic store the SAME shape: "If,
      *> after decimal point alignment, the number of places in the fractional part of the result of an
      *> arithmetic operation is greater than the number of places provided for the fraction of the resultant
      *> identifier, truncation is relative to the size provided for the resultant identifier."  14.7.4.3
      *> rule 2: "If the ROUNDED phrase is not specified, execution is as if ROUNDED MODE IS TRUNCATION had
      *> been specified", and rule 10 spells TRUNCATION out as "the nearest value nearer to zero".  8.8.1.3's
      *> native latitude ("Native arithmetic is an implementor-defined method of evaluating an arithmetic
      *> expression, an arithmetic statement, the SUM clause, and all integer and numeric functions") covers
      *> the INTERMEDIATE, never the rounding of the transfer that 14.7.4.1/14.7.4.3 fix.
      *>
      *> EVERY EXPECTED VALUE IS THE EXACT DECIMAL EXPANSION OF THE BINARY64, COMPUTED, NEVER OBSERVED:
      *>   sqrt(3)            = 1.732050807568877193176604123436845839023590087890625
      *>       scale 9 -> the discarded tail begins with 5, so TRUNCATION 1.732050807 and every NEAREST
      *>       mode 1.732050808; TOWARD-GREATER 1.732050808, TOWARD-LESSER 1.732050807 (the value is > 0).
      *>       scale 4 -> 1.7320 truncated, 1.7321 rounded.
      *>   sqrt(0.9999999999) = 0.99999999994999999586298145004548132419586181640625
      *>       scale 1 -> 0.9 truncated, 1.0 rounded: ONE returned value split across a whole tenth.
      *>   sqrt(4)            = 2 exactly, so a PROHIBITED store of it is exact and commits.
      *> 3 ** 0.5 is the SAME binary64 as FUNCTION SQRT(3) and takes the SAME landing -- the native
      *> exponentiation quantizer is the float family's, and it carried the identical hard-coded mode.
      *>
      *> DEFAULT ROUNDED MODE (11.9.6, via 14.7.4.3 rule 1) is TOWARD-GREATER here, so a BARE ROUNDED rounds
      *> up; a statement with NO ROUNDED phrase still truncates (rule 2 defers to the clause only when the
      *> phrase is present), which is the leg that proves the default is not leaking into the no-phrase store.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB647FM3.
       OPTIONS.
           DEFAULT ROUNDED MODE IS TOWARD-GREATER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R9  PIC 9V9(9).
       01 R1  PIC 9V9.
       01 R4  PIC 9V9(4).
       01 SE  PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
      *> A -- the defect: one returned value, two channels, one receiver.
           MOVE FUNCTION SQRT(3) TO R9
           DISPLAY "A1=" R9
           COMPUTE R9 = FUNCTION SQRT(3)
           DISPLAY "A2=" R9
      *> B -- the ROUNDED phrase now reaches the family, and each named mode is its own answer.
           COMPUTE R9 ROUNDED MODE IS NEAREST-AWAY-FROM-ZERO = FUNCTION SQRT(3)
           DISPLAY "B1=" R9
           COMPUTE R9 ROUNDED MODE IS TOWARD-LESSER = FUNCTION SQRT(3)
           DISPLAY "B2=" R9
           COMPUTE R9 ROUNDED MODE IS TRUNCATION = FUNCTION SQRT(3)
           DISPLAY "B3=" R9
      *> C -- the bare ROUNDED takes the OPTIONS default; the no-phrase store does not.
           COMPUTE R9 ROUNDED = FUNCTION SQRT(3)
           DISPLAY "C1=" R9
      *> D -- the split across a whole tenth, both channels.
           MOVE FUNCTION SQRT(0.9999999999) TO R1
           DISPLAY "D1=" R1
           COMPUTE R1 = FUNCTION SQRT(0.9999999999)
           DISPLAY "D2=" R1
           COMPUTE R1 ROUNDED MODE IS NEAREST-AWAY-FROM-ZERO
               = FUNCTION SQRT(0.9999999999)
           DISPLAY "D3=" R1
      *> E -- native ** is the same quantizer and takes the same landing.
           COMPUTE R9 = 3 ** 0.5
           DISPLAY "E1=" R9
           COMPUTE R9 ROUNDED MODE IS NEAREST-AWAY-FROM-ZERO = 3 ** 0.5
           DISPLAY "E2=" R9
      *> F -- ROUNDED MODE IS PROHIBITED asks 14.7.4.3 rule 7 of the resultant identifier: an inexact
      *> transfer sets EC-SIZE-TRUNCATION and leaves the receiver UNCHANGED; an exact one commits.
           MOVE ZERO TO R9
           MOVE "---" TO SE
           COMPUTE R9 ROUNDED MODE IS PROHIBITED = FUNCTION SQRT(3)
               ON SIZE ERROR MOVE "SIZ" TO SE
               NOT ON SIZE ERROR MOVE "OK " TO SE
           END-COMPUTE
           DISPLAY "F1=" SE " " R9
           COMPUTE R9 ROUNDED MODE IS PROHIBITED = FUNCTION SQRT(4)
               ON SIZE ERROR MOVE "SIZ" TO SE
               NOT ON SIZE ERROR MOVE "OK " TO SE
           END-COMPUTE
           DISPLAY "F2=" SE " " R9
      *> G -- a NESTED float operand is not the final transfer: it lands truncated at the working scale and
      *> the ONE receiver store performs the statement's rounding (14.7.4.3 rules 3-10 each name "the
      *> resultant identifier"). At scale 4 the working scale still carries five spare digits, so both
      *> answers are the exact expansion's, rounded once.
           COMPUTE R4 = FUNCTION SQRT(3) * 1
           DISPLAY "G1=" R4
           COMPUTE R4 ROUNDED MODE IS NEAREST-AWAY-FROM-ZERO
               = FUNCTION SQRT(3) * 1
           DISPLAY "G2=" R4
           STOP RUN.
