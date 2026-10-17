*>--- conformance-case
*> id: pb253_prose_float_sdidi_container_2023
*> rules: [RV-15.89.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB253, the 2023 (default --std) edge — ARITHMETIC IS STANDARD-DECIMAL, the same container
      *> rule as the 2014 twin: ISO 15.4.1 "When standard-decimal arithmetic or standard-binary arithmetic is
      *> in effect, the returned value for numeric and integer functions is contained in a temporary standard
      *> data item in the intermediate form defined for the arithmetic mode in effect", with 8.8.1.5.1 making
      *> the mode "a method of evaluating an arithmetic expression, an arithmetic statement, the SUM clause,
      *> and certain integer and numeric functions as specified in 15.4.1". The prose family (ACOS/ASIN/ATAN/
      *> COS/SIN/TAN/LOG/LOG10/RANDOM) has no equivalent arithmetic expression, so 15.4.1's last paragraph
      *> leaves its VALUE implementor-defined — but not its CONTAINER, which is what the receiver-shape-first
      *> arm order bypassed for every receiver-less or float-receiver reference.
      *>
      *> HAND-DERIVED: TINY is the exact scale-20 value 1E-20; for that argument the binary64 sin/tan/atan
      *> results ARE x (the x**3/3 term is ~25 decades below x's ULP), and the 8.8.1.5.1 float -> SDIDI
      *> conversion is the shortest round-trip decimal identity, so the SDIDI is exactly 1E-20 and its
      *> item-92 text is 0.00000000000000000001, never the binary64 E-notation "1E-20".
      *> ACOS(1) / ASIN(0) / LOG(1) / LOG10(1) are 0 exactly and COS(0) is 1 exactly.
      *> The large-magnitude leg pins 15.4.1's invariant instead of an implementor-defined digit string: "the
      *> returned value is the same for all instances of a given function within a single execution of the
      *> runtime element so long as the value and order of the arguments, the collating sequence, and the
      *> locale are unchanged" — before the fix the MOVE channel landed 16331239353195368.96 and the
      *> arithmetic-receiver channel 16331239353195370.00 for the SAME call.
      *> NEARHALF is pi/2 to 30 places, so its tangent is the reciprocal of NEARHALF - pi/2 = -7.51E-31:
      *> 1.3308E+30, which B1 / B2 hold in 31 integer digits (kb/Work PB1041 - the body used to answer
      *> for NEARHALF's binary64, 1.63E+16, and a receiver sized for THAT value truncated the real one).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB253S3.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TINY     PIC SV9(20) VALUE 0.00000000000000000001.
       01 NEARHALF PIC S9V9(30) VALUE 1.570796326794896619231321691639.
       01 T1       PIC SV9(20).
       01 T2       PIC SV9(20).
       01 B1       PIC S9(31).
       01 B2       PIC S9(31).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "SIN-T=" FUNCTION SIN(TINY)
           DISPLAY "ATAN-T=" FUNCTION ATAN(TINY)
           DISPLAY "ACOS-T=" FUNCTION ACOS(1)
           DISPLAY "COS-T=" FUNCTION COS(0)
           MOVE FUNCTION SIN(TINY) TO T1
           COMPUTE T2 = FUNCTION SIN(TINY)
           DISPLAY "SIN-MOVE=" T1
           DISPLAY "SIN-COMP=" T2
           MOVE FUNCTION TAN(NEARHALF) TO B1
           COMPUTE B2 = FUNCTION TAN(NEARHALF)
           IF B1 = B2
               DISPLAY "TAN-CHANNELS-AGREE"
           ELSE
               DISPLAY "TAN-CHANNELS-DIFFER " B1 " " B2
           END-IF
           IF FUNCTION TAN(NEARHALF) = B1
               DISPLAY "RELATION-CHANNEL-AGREES"
           ELSE
               DISPLAY "RELATION-CHANNEL-DIFFERS"
           END-IF
           STOP RUN.
