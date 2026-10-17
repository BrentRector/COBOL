*>--- conformance-case
*> id: pb253_prose_float_standard_container
*> rules: [RV-15.89.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB253, the 2002 edge — ARITHMETIC IS STANDARD (11.9.5; the 2002 mode, obsolete at 2014 and
      *> removed at 2023) uses the standard intermediate data item, which for these operands IS the
      *> standard-decimal form, so it reaches the SAME container rule as STANDARD-DECIMAL: ISO 15.4.1 "When
      *> standard-decimal arithmetic or standard-binary arithmetic is in effect, the returned value for numeric
      *> and integer functions is contained in a temporary standard data item in the intermediate form defined
      *> for the arithmetic mode in effect". The renderer's mode predicate is the ONE set
      *> (ArithmeticModes.IsDecimalEngine), and this golden is what keeps the 2002 spelling inside it: the
      *> defect made the SDIDI arm unreachable for every receiver-less or float-receiver reference at BOTH
      *> spellings, and a 2014-only golden would have proved only half of that.
      *>
      *> HAND-DERIVED: TINY is the exact scale-20 value 1E-20. For that argument sin x = x - x**3/6 and
      *> tan x = x + x**3/3 with |x**3/3| ~ 3.3E-61, some 25 decades below the ULP of x (~1.6E-36), so each
      *> binary64 result IS x. The 8.8.1.5.1 float -> SDIDI conversion is the shortest round-trip decimal
      *> identity, so the SDIDI is exactly 1E-20 and its 15.4.1 item-92 text is the fixed-point digit string
      *> 0.00000000000000000001 — never the binary64 E-notation "1E-20" the un-contained value printed. The
      *> scale-20 store of that SDIDI is exact: unscaled 1, DISPLAY image 19 zeros then the overpunched +1.
      *> The MOVE-source and arithmetic-receiver channels must then agree, per 15.4.1's "the returned value is
      *> the same for all instances of a given function within a single execution of the runtime element so
      *> long as the value and order of the arguments, the collating sequence, and the locale are unchanged".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB253ST.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TINY PIC SV9(20) VALUE 0.00000000000000000001.
       01 T1   PIC SV9(20).
       01 T2   PIC SV9(20).
       01 FL   USAGE COMP-2.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "SIN-T=" FUNCTION SIN(TINY)
           DISPLAY "TAN-T=" FUNCTION TAN(TINY)
           MOVE FUNCTION SIN(TINY) TO T1
           COMPUTE T2 = FUNCTION SIN(TINY)
           DISPLAY "SIN-MOVE=" T1
           DISPLAY "SIN-COMP=" T2
           MOVE FUNCTION SIN(TINY) TO FL
           IF FL = FUNCTION SIN(TINY)
               DISPLAY "FLOAT-CHANNEL-AGREES"
           ELSE
               DISPLAY "FLOAT-CHANNEL-DIFFERS"
           END-IF
           STOP RUN.
