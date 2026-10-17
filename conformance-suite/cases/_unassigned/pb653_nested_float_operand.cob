      *>--- conformance-case
      *> id: pb653_nested_float_operand
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB653 at COBOL-85 -- A NESTED FLOAT OPERAND IS NOT A TRANSFER, SO IT IS NOT QUANTIZED.
      *> PB647 made the float->fixed landing's SCALE and MODE one decision and gave a NESTED operand the
      *> >=9 float working scale with truncation. That truncates the returned value one digit too early and
      *> every operation above it propagates the loss, so ONE binary64 gave TWO answers depending only on
      *> whether it arrived as a function's returned value or out of a COMP-2 item holding the same double:
      *>   COMPUTE T9 = FUNCTION SQRT(3) * 2   gave 003.464101614
      *>   COMPUTE T9 = C2 * 2   (C2 = SQRT(3)) gave 003.464101615
      *> ISO 15.4.1 forbids the split outright -- "the returned value is the same for all instances of a
      *> given function within a single execution of the runtime element so long as the value and order of
      *> the arguments, the collating sequence, and the locale are unchanged" -- and the RELATION channel,
      *> which never quantized, already agreed with the COMP-2 item. 8.8.1.3 grants native arithmetic "an
      *> implementor-defined method of evaluating an arithmetic expression, an arithmetic statement, the SUM
      *> clause, and all integer and numeric functions", which licenses a working scale but not two working
      *> scales for one value; 14.7.4.1 puts the one truncation at the transfer -- "If, after decimal point
      *> alignment, the number of places in the fractional part of the result of an arithmetic operation is
      *> greater than the number of places provided for the fraction of the resultant identifier, truncation
      *> is relative to the size provided for the resultant identifier" -- and 14.7.4.3 rule 2 makes the
      *> no-phrase store "as if ROUNDED MODE IS TRUNCATION had been specified".
      *>
      *> EVERY EXPECTED VALUE IS THE EXACT DECIMAL EXPANSION OF THE BINARY64, COMPUTED, NEVER OBSERVED:
      *>   sqrt(3)         = 1.732050807568877193176604123436845839023590087890625
      *>   sqrt(3) * 2     = 3.46410161513775438635320824687369167804718017578125      -> 3.464101615
      *>   sqrt(10) ** 2   = 10.0000000000000017763568394002504646778106689453125      -> 10.000000000
      *>   sqrt(10)*sqrt(10) is the SAME double as sqrt(10) ** 2                       -> 10.000000000
      *>   (sqrt(3)*2)*3   = 10.392304845413264047238044440746307373046875             -> 10.392304845
      *>   sqrt(2) / 3     = 0.471404520791031733661924363332218490540981292724609375  -> 0.471404520
      *>                                                        rounded at 9 digits    -> 0.471404521
      *>   sqrt(3) * 1     = sqrt(3)            truncated at 9 -> 1.732050807, rounded -> 1.732050808
      *> The paired arms are the PROOF: each Nx line must equal its N(x-1) line, because the two operands
      *> are the same binary64 reached two ways. The nested arm alone used to answer ...614 / 9.999999998 /
      *> 10.392304842 / 1.732050807.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB653NO85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R9  PIC 9V9(9).
       01 T9  PIC 9(3)V9(9).
       01 C2  USAGE COMP-2.
       PROCEDURE DIVISION.
       MAIN.
      *> A -- the defect itself: one binary64, two operand positions, one receiver.
           COMPUTE C2 = FUNCTION SQRT(3)
           COMPUTE T9 = C2 * 2
           DISPLAY "A1=" T9
           COMPUTE T9 = FUNCTION SQRT(3) * 2
           DISPLAY "A2=" T9
      *> B -- native ** reads the SAME landing decision, and had the same missing arm.
           COMPUTE C2 = FUNCTION SQRT(10)
           COMPUTE T9 = C2 ** 2
           DISPLAY "B1=" T9
           COMPUTE T9 = FUNCTION SQRT(10) ** 2
           DISPLAY "B2=" T9
      *> C -- TWO nested float operands in one product: the shape a guard-digit working scale could not
      *> survive (two guarded scale-23 operands multiply into a scale-46 product the Int128 carrier wraps).
           COMPUTE T9 = C2 * C2
           DISPLAY "C1=" T9
           COMPUTE T9 = FUNCTION SQRT(10) * FUNCTION SQRT(10)
           DISPLAY "C2=" T9
      *> D -- a chain of three operands: the loss used to compound along it.
           COMPUTE C2 = FUNCTION SQRT(3)
           COMPUTE T9 = C2 * 2 * 3
           DISPLAY "D1=" T9
           COMPUTE T9 = FUNCTION SQRT(3) * 2 * 3
           DISPLAY "D2=" T9
      *> E -- a nested QUOTIENT of a float operand takes the same native lane.
           COMPUTE C2 = FUNCTION SQRT(2)
           COMPUTE R9 = C2 / 3
           DISPLAY "E1=" R9
           COMPUTE R9 = FUNCTION SQRT(2) / 3
           DISPLAY "E2=" R9
      *> F -- and the ROUNDED phrase still binds to the transfer into the resultant identifier, applied
      *> ONCE there (14.7.4.3 rules 3-10 each name "the resultant identifier"): the nested operand carries
      *> no rounding of its own, so F1 and F2 round the SAME double and F3 truncates it.
           COMPUTE C2 = FUNCTION SQRT(3)
           COMPUTE R9 ROUNDED = FUNCTION SQRT(3) * 1
           DISPLAY "F1=" R9
           COMPUTE R9 ROUNDED = C2 * 1
           DISPLAY "F2=" R9
           COMPUTE R9 = FUNCTION SQRT(3) * 1
           DISPLAY "F3=" R9
      *> G -- the receiver-less channel is the fourth spelling of the one returned value, and it agreed
      *> with the COMP-2 item all along; both arms must still say so. Both answer NE, and that is the
      *> CORRECT native answer: 8.8.4.2.4 -- "When native arithmetic is in effect, comparison proceeds by
      *> the rules of native arithmetic" -- and neither binary64 square is the integer exactly
      *> (sqrt(10)*sqrt(10) = 10.0000000000000017763568394002504646778106689453125,
      *>  sqrt(3)*sqrt(3)   = 2.999999999999999555910790149937383830547332763671875).
           IF FUNCTION SQRT(10) * FUNCTION SQRT(10) = 10
               DISPLAY "G1=EQ"
           ELSE
               DISPLAY "G1=NE"
           END-IF
           IF C2 * C2 = 3
               DISPLAY "G2=EQ"
           ELSE
               DISPLAY "G2=NE"
           END-IF
           STOP RUN.
