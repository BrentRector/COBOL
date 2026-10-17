      *>--- conformance-case
      *> id: pb157_compute_bit_group
      *> rules: [SR-14.9.8.3-2, SR-14.9.8.3-3, GR-14.9.8.4-3, FMT-14.9.8.2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB157CB.
      *> kb/Work PB157 - COMPUTE Format 2 over the bit-group model.
      *> 14.9.8.3 SR2 wants an ELEMENTARY boolean receiver and
      *> 13.18.29.4 GR1b makes a GROUP-USAGE BIT group exactly that
      *> (as-if PICTURE 1(m)) - the binder's raw-Pic readers REJECTED it
      *> while the sending side of the same statement accepted it.
      *> Legs: H1 - F2 direct with a bit-group receiver AND operand
      *> (GR3 width = max item positions = 6); HZ - bare figurative
      *> ZERO with a boolean receiver is LEGAL (8.8.2 lists ZERO and
      *> the ALL literal as DISJOINT operands; only the Format-6
      *> ALL B"..." falls under SR3's ban - this rejected before);
      *> H2 - the F1->F2 reroute on a sole bit-group reference; M1/M2 -
      *> multi-target Format 2 with END-COMPUTE (never once written);
      *> B3/B8 - a 6-position value truncates right into 1(3) and
      *> zero-fills into 1(8) per 14.6.8.6; W8 - the DISCRIMINATING
      *> mixed-width leg: A3 right-zero-extends to 6 (8.8.2 rule 9),
      *> the GR3 width is the LARGEST ITEM (6, the bit group) - a
      *> Gr3Width that counted the group as 0 resized to 3 and answered
      *> 11000000; AZ - ALL ZERO is Format 1's OPTIONAL-ALL figurative
      *> ZERO (8.3.3.6.2), never the ALL literal; NZ - B-NOT ZERO folds
      *> positionless all-ones and replicates to the receiver width
      *> (8.3.3.6.4 GR2); ZB - ZERO as ONE operand of a binary op is
      *> legal (8.8.2 rule 4 bars only BOTH being ALL literals).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G GROUP-USAGE BIT.
          05 GA PIC 1(2) VALUE B"10".
          05 GB PIC 1(4) VALUE B"0110".
       01 H GROUP-USAGE BIT.
          05 HA PIC 1(6) VALUE B"000000".
       01 B6 PIC 1(6) USAGE BIT VALUE B"111111".
       01 B3 PIC 1(3) USAGE BIT VALUE B"111".
       01 B8 PIC 1(8) USAGE BIT.
       01 A3 PIC 1(3) USAGE BIT VALUE B"010".
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE H = G B-AND B6
           DISPLAY "H1=[" H "]"
           COMPUTE H = ZERO
           DISPLAY "HZ=[" H "]"
           COMPUTE H = G
           DISPLAY "H2=[" H "]"
           COMPUTE B6 H = G B-XOR G END-COMPUTE
           DISPLAY "M1=[" B6 "] M2=[" H "]"
           COMPUTE B3 = G B-OR B"000111"
           DISPLAY "B3=[" B3 "]"
           COMPUTE B8 = G B-OR B"000111"
           DISPLAY "B8=[" B8 "]"
           COMPUTE B8 = G B-OR A3
           DISPLAY "W8=[" B8 "]"
           COMPUTE H = ALL ZERO
           DISPLAY "AZ=[" H "]"
           COMPUTE H = B-NOT ZERO
           DISPLAY "NZ=[" H "]"
           COMPUTE H = ZERO B-AND G
           DISPLAY "ZB=[" H "]"
           STOP RUN.
