*>--- conformance-case
*> id: pb253_prose_float_sdidi_container_2014
*> rules: [RV-15.89.4-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB253 — under ARITHMETIC IS STANDARD-DECIMAL the PROSE float family's returned value is
      *> CONTAINED IN an SDIDI in EVERY reference context, not only when a fixed-point arithmetic receiver
      *> happens to be present. ISO 15.4.1: "When standard-decimal arithmetic or standard-binary arithmetic is
      *> in effect, the returned value for numeric and integer functions is contained in a temporary standard
      *> data item in the intermediate form defined for the arithmetic mode in effect" — unconditional on the
      *> shape of whatever consumes the value; and 8.8.1.5.1 makes the mode "a method of evaluating an
      *> arithmetic expression, an arithmetic statement, the SUM clause, and certain integer and numeric
      *> functions as specified in 15.4.1". 15.4.1's last paragraph ("When a numeric or integer function does
      *> not have an equivalent arithmetic expression, its returned value is implementor-defined") exempts the
      *> VALUE — the binary64 approximation this family computes — never the CONTAINER.
      *> The family with no equivalent arithmetic expression and no SDIDI body is ACOS/ASIN/ATAN/COS/SIN/TAN/
      *> LOG/LOG10/RANDOM; the defect made the SDIDI arm unreachable for every receiver-less or float-receiver
      *> reference, so the raw binary64 escaped into the text, MOVE-source, float-receiver and relation channels.
      *>
      *> HAND-DERIVED EXPECTATIONS.
      *> TINY is the exact scale-20 value 1E-20; ScaledToDouble gives the binary64 nearest 1E-20. For that
      *> argument x, sin x = x - x**3/6, tan x = x + x**3/3 and atan x = x - x**3/3, and |x**3/3| ~ 3.3E-61 is
      *> some 25 decades below the ULP of x (~1.6E-36), so each rounds to x itself in binary64. The 8.8.1.5.1
      *> float -> SDIDI conversion is the shortest round-trip decimal identity, so the SDIDI is exactly 1E-20
      *> and its 15.4.1 item-92 text is the fixed-point digit string 0.00000000000000000001 — NEVER the
      *> binary64 E-notation "1E-20" the un-contained value printed.
      *> COS(0) = 1 exactly, and ACOS(1) / ASIN(0) / LOG(1) / LOG10(1) are 0 exactly (each an exact
      *> binary64 result), so their SDIDI texts are "1" and "0".
      *> Storing the SDIDI 1E-20 into PIC SV9(20) is exact: unscaled 1, DISPLAY image 19 zeros then the
      *> overpunched +1 ("A").
      *> The large-magnitude legs pin the 15.4.1 INVARIANT rather than an implementor-defined digit string:
      *> "the returned value is the same for all instances of a given function within a single execution of the
      *> runtime element so long as the value and order of the arguments, the collating sequence, and the locale
      *> are unchanged". Before the fix `MOVE FUNCTION TAN(NEARHALF)` landed 16331239353195368.96 (the
      *> binary64 x 10**scale artifact) while `COMPUTE ... = FUNCTION TAN(NEARHALF)` landed 16331239353195370.00
      *> — two returned values for one function and one argument in one run.
      *> RANDOM's seeded form restarts the sequence (15.75.3), so two seeded references share an argument and
      *> owe the same returned value in both channels.
      *> NEARHALF is pi/2 to 30 places, so its tangent is the reciprocal of NEARHALF - pi/2 = -7.51E-31:
      *> 1.3308E+30, which B1 / B2 hold in 31 integer digits (kb/Work PB1041 - the body used to answer
      *> for NEARHALF's binary64, 1.63E+16, and a receiver sized for THAT value truncated the real one).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB253SD.
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
       01 FL       USAGE COMP-2.
       PROCEDURE DIVISION.
       MAIN.
      *> 1 — the item-92 TEXT channel renders the SDIDI, not a binary64.
           DISPLAY "SIN-T=" FUNCTION SIN(TINY)
           DISPLAY "TAN-T=" FUNCTION TAN(TINY)
           DISPLAY "ATAN-T=" FUNCTION ATAN(TINY)
           DISPLAY "COS-T=" FUNCTION COS(0)
           DISPLAY "ACOS-T=" FUNCTION ACOS(1)
           DISPLAY "ASIN-T=" FUNCTION ASIN(0)
           DISPLAY "LOG-T=" FUNCTION LOG(1)
           DISPLAY "LOG10-T=" FUNCTION LOG10(1)
      *> 2 — the MOVE-source channel and the arithmetic-receiver channel deliver ONE returned value.
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
      *> 3 — the float-receiver channel and the relation channel see that same value.
           MOVE FUNCTION SIN(TINY) TO FL
           IF FL = FUNCTION SIN(TINY)
               DISPLAY "FLOAT-CHANNEL-AGREES"
           ELSE
               DISPLAY "FLOAT-CHANNEL-DIFFERS"
           END-IF
           IF FUNCTION TAN(NEARHALF) = B1
               DISPLAY "RELATION-CHANNEL-AGREES"
           ELSE
               DISPLAY "RELATION-CHANNEL-DIFFERS"
           END-IF
      *> 4 — RANDOM (15.75.3 seeded form) across the same two channels.
           MOVE FUNCTION RANDOM(7) TO T1
           COMPUTE T2 = FUNCTION RANDOM(7)
           IF T1 = T2
               DISPLAY "RANDOM-CHANNELS-AGREE"
           ELSE
               DISPLAY "RANDOM-CHANNELS-DIFFER " T1 " " T2
           END-IF
           STOP RUN.
