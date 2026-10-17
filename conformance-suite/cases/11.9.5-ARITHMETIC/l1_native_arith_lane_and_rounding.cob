*>--- conformance-case
*> id: l1_native_arith_lane_and_rounding
*> rules: [GR-11.9.5.2-4, DOC-A.1-123]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.8.1.3 Native arithmetic — the two halves of Annex A.1 item 123's determination that the
      *> intermediate's SIZE cannot reach (docs/CONFORMANCE.md#DOC-A.1-123). Its sibling
      *> conformance:cases/11.9.5-ARITHMETIC/l1_native_arithmetic_intermediate measures the scaled-Int128 carrier at the §14.7.7
      *> rule 2a boundary, and conformance:cases/15.4.1-Numeric-and-integer-functions/pb125_factorial_native_size measures its magnitude ceiling from
      *> both sides. NEITHER touches the two sentences below, and this file exists for them.
      *>   python scripts/spec/cite.py --check 8.8.1.3 "The implementor shall specify techniques used for native
      *>   arithmetic."  ->  OK  §8.8.1.3
      *>   python scripts/spec/cite.py --check 14.7.7 "Otherwise, an implementor-defined intermediate data item
      *>   is used."  ->  OK  §14.7.7 4) a)
      *> No OPTIONS paragraph is written, so §11.9.5.2 GR4 puts native arithmetic in effect — the mode every
      *> other program in the corpus compiles under:
      *>   python scripts/spec/cite.py --check 11.9.5.2 "it is as if the ARITHMETIC clause were specified with
      *>   the NATIVE phrase"  ->  OK  §11.9.5.2 4)
      *>
      *> ── HALF ONE: THE FLOAT LANE. Row 123: "Any expression with a floating-point operand evaluates ENTIRELY
      *> in IEEE binary64 instead." That is a lane switch, not a widening, and 2**53 is where the two lanes part
      *> company: 60559:2020's binary64 significand is 53 bits, so 2**53 + 1 = 9 007 199 254 740 993 is the
      *> smallest positive integer it cannot represent, and round-to-nearest-ties-to-even answers 2**53.
      *>   NAT53   9007199254740992 + 1 with no float operand anywhere: the scaled-Int128 intermediate is exact,
      *>           so the answer is 9007199254740993. (§14.9.2.4 GR4's "enough places shall be carried so as not
      *>           to lose any significant digits" is satisfied a fortiori — the value is 16 digits.)
      *>   FLT53   ⛔ THE MEASUREMENT. The SAME expression plus `+ FZ`, where FZ is a FLOAT-LONG item holding
      *>           ZERO. Adding zero changes no value; it changes the LANE, and the answer drops to
      *>           9007199254740992. An implementation that converted only the float OPERAND and kept the exact
      *>           lane for the rest would answer ...993 here and would not be doing what row 123 documents.
      *>           This is also the leg that proves the determination is a real choice with a real cost, which
      *>           is exactly what A.1 requires an implementor to write down.
      *>
      *> ── HALF TWO: ONE ROUNDING, AT THE FINAL TRANSFER. Row 123: "a nested quotient carries up to 14 guard
      *> fraction digits and rounds once, at the final transfer". The standard's own words for the second half:
      *>   python scripts/spec/cite.py --check 14.7.7 "The ROUNDED phrase applies only to this transfer of data."
      *>   ->  OK  §14.7.7 3)   (NOTE 1 to rule 3)
      *> and the mode a bare ROUNDED carries:
      *>   python scripts/spec/cite.py --check 11.9.6.3 "If the DEFAULT ROUNDED clause is not specified, DEFAULT
      *>   ROUNDED MODE IS NEAREST-AWAY-FROM-ZERO is implied."  ->  OK  §11.9.6.3 2)
      *> ONE / THR is NESTED (the multiplication consumes it), so it is not the transfer and it must NOT round:
      *> it truncates at the receiver's scale plus guard digits, and the receiver's ROUNDED then applies ONCE to
      *> the whole expression's value.
      *>   TRUNC   no ROUNDED phrase: the quotient's guard digits are all 3s, the product's all 9s, and the
      *>           transfer TRUNCATES to five fraction digits — 0.99999, imaged as 099999 by the V.
      *>   ROUND1  ⛔ THE MEASUREMENT. The same expression WITH ROUNDED. The value reaching the transfer is
      *>           0.999… to well past five places, so NEAREST-AWAY-FROM-ZERO carries it to 1.00000 — imaged as
      *>           100000. An implementation that applied the receiver's rounding to the NESTED quotient instead
      *>           would round 1/3 to 0.33333, multiply by 3 to 0.99999, and answer 099999 for BOTH lines: the
      *>           two legs would be indistinguishable, which is why they are written as a pair.
      *>           (Any guard width at all, and any rounding mode other than truncation, gives 100000 — the leg
      *>           measures WHERE the rounding happens, not how many guard digits there are.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NATLNE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N16 PIC 9(16).
       01 FZ  USAGE FLOAT-LONG VALUE 0.
       01 ONE PIC 9 VALUE 1.
       01 THR PIC 9 VALUE 3.
       01 Q   PIC 9V9(5).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N16 = 9007199254740992 + 1
           DISPLAY "NAT53=" N16
           COMPUTE N16 = 9007199254740992 + 1 + FZ
           DISPLAY "FLT53=" N16
           COMPUTE Q = ONE / THR * THR
           DISPLAY "TRUNC=" Q
           COMPUTE Q ROUNDED = ONE / THR * THR
           DISPLAY "ROUND1=" Q
           STOP RUN.
