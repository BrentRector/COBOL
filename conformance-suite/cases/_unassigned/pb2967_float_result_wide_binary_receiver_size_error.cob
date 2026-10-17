*>--- conformance-case
*> id: pb2967_float_result_wide_binary_receiver_size_error
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2967 - a binary64 result too large for ANY container is a SIZE ERROR in a 16-byte COMP-5 receiver.
      *>
      *> 14.7.5 case 3: the size error condition exists "if, after radix point alignment and any applicable rounding
      *> specifications, the result of an arithmetic statement is further from zero than permitted for the
      *> associated resultant data item" (cite.py --check 14.7.5 "further from zero than permitted" -> OK 14.7.5 3)),
      *> and 14.7.5 rule 1 leaves every resultant unchanged then: "If the size error condition occurred during the
      *> arithmetic operations specified by the arithmetic statement, the values of all of the resultant data items
      *> remain unchanged from the values they had at the start of the" statement (cite.py --check 14.7.5 "unchanged"
      *> -> OK 14.7.5 1)).
      *>
      *> The container of a 16-byte COMP-5 item is 128 bits (13.18.60.4 - a binary item of PIC S9(19) here), so its
      *> largest value is 2**127 - 1 = 170141183460469231731687303715884105727, about 1.7E+38.  Every result below is
      *> larger than that, derived by hand:
      *>   VARIANCE(A, 1), A = 10**29: mean = (10**29 + 1) / 2, variance = ((A - 1) / 2)**2 = about 2.5E+57 (15.98.4).
      *>   EXP10(45) = 10**45 (15.35.4 r1: the equivalent expression 10 ** argument-1).
      *>   A ** 1.5 = 10**43.5 = about 3.2E+43.
      *>   F1 = 1.0E+40 and F2 = -1.0E+40 (FLOAT-LONG items).
      *> The old checked float landing saturated an oversized value to Int128.MaxValue, which a 16-byte receiver
      *> accepts as a legal value, so every one of these took NOT ON SIZE ERROR and stored 2**127 - 1.  Each leg
      *> therefore fails the old code in a different place: the intrinsic quantizer (VARIANCE, EXP10), native **,
      *> and the COMP-2 operand's own landing.  The PIC S9(18) DISPLAY control always raised (its capacity is far
      *> below the carrier) and must keep doing so; the in-range controls must keep storing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2967W.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9(30) VALUE 100000000000000000000000000000.
       01 F1 COMP-2 VALUE 1.0E40.
       01 F2 COMP-2 VALUE -1.0E40.
       01 RS PIC S9(19) COMP-5 VALUE 7.
       01 RU PIC 9(19) COMP-5 VALUE 7.
       01 RFX PIC S9(19)V99 COMP-5 VALUE 7.
       01 R18 PIC S9(18) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE RS = FUNCTION VARIANCE(A, 1)
               ON SIZE ERROR DISPLAY "VARIANCE-SIGNED SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "VARIANCE-SIGNED STORED"
           END-COMPUTE
           DISPLAY "RS=" RS
           COMPUTE R18 = FUNCTION VARIANCE(A, 1)
               ON SIZE ERROR DISPLAY "VARIANCE-DISPLAY SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "VARIANCE-DISPLAY STORED"
           END-COMPUTE
           DISPLAY "R18=" R18
           COMPUTE RU = FUNCTION EXP10(45)
               ON SIZE ERROR DISPLAY "EXP10-UNSIGNED SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "EXP10-UNSIGNED STORED"
           END-COMPUTE
           DISPLAY "RU=" RU
           COMPUTE RFX = FUNCTION EXP10(45)
               ON SIZE ERROR DISPLAY "EXP10-SCALED SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "EXP10-SCALED STORED"
           END-COMPUTE
           DISPLAY "RFX=" RFX
           COMPUTE RS = A ** 1.5
               ON SIZE ERROR DISPLAY "POWER SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "POWER STORED"
           END-COMPUTE
           DISPLAY "RS=" RS
           COMPUTE RS = F1 * 1
               ON SIZE ERROR DISPLAY "COMP2-POSITIVE SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "COMP2-POSITIVE STORED"
           END-COMPUTE
           DISPLAY "RS=" RS
           COMPUTE RS = F2 * 1
               ON SIZE ERROR DISPLAY "COMP2-NEGATIVE SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "COMP2-NEGATIVE STORED"
           END-COMPUTE
           DISPLAY "RS=" RS
           COMPUTE RS = FUNCTION EXP10(18)
               ON SIZE ERROR DISPLAY "EXP10-INRANGE SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "EXP10-INRANGE STORED"
           END-COMPUTE
           DISPLAY "RS=" RS
           COMPUTE RU = F1 / F1
               ON SIZE ERROR DISPLAY "COMP2-INRANGE SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "COMP2-INRANGE STORED"
           END-COMPUTE
           DISPLAY "RU=" RU
           STOP RUN.
