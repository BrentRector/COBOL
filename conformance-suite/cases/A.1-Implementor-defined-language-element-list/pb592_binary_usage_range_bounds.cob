      *>--- conformance-case
      *> id: pb592_binary_usage_range_bounds
      *> rules: [DOC-A.1-206]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.18.60.4 GR12 - the BINARY-CHAR / BINARY-SHORT /
      *> BINARY-LONG / BINARY-DOUBLE minimum ranges, and the Annex A.1
      *> item 206 determination: this implementation DOES "allow a
      *> wider range" than the minimum, by exactly one value per
      *> SIGNED usage.  Witness for the docs/CONFORMANCE.md section 7
      *> row DOC-A.1-206 (kb/Work PB592; owner decision Q30,
      *> 2026-09-09).
      *>
      *> It lives in the 2002 corpus because that is the edition that
      *> INTRODUCES the four usages (constructs.json
      *> usage-binary-char-family-2002, introducedIn 2002; below 2002
      *> the introduction gate COBOLNET0900 names the edition, which
      *> the version matrix already drives).  The band itself has no
      *> edition input at all - it is PicInfo.StorageWidth plus
      *> PicInfo.Truncation - so pinning it at the introducing edition
      *> pins it at 2014 and 2023 too.
      *>
      *> GR12 (cite.py --check 13.18.60.4 OK): "The minimum range
      *> requirements for each of these usages are shown below.  Any
      *> integer value of n within the specified range shall be
      *> expressible in the given usage.  The implementor may allow a
      *> wider range.  However, for signed items, any number that may
      *> be expressed as BINARY-CHAR shall also be expressible as
      *> BINARY-SHORT, ... as BINARY-LONG, ... as BINARY-DOUBLE."
      *>
      *> THE PRINTED TABLE IS ASYMMETRIC AND THAT ASYMMETRY IS THE
      *> WHOLE RULE.  Re-rendered from the canonical PDF for this
      *> landing (scripts/render-spec-page.py, PDF page 537 = printed
      *> page 507): every SIGNED row reads "-128 [-2**7]  < n <  128
      *> [2**7]" - a STRICT less-than on BOTH sides - while every
      *> UNSIGNED row reads "0  <= n <  256 [2**8]".  So the band a
      *> conforming processor MUST express is
      *>     BINARY-CHAR   SIGNED   -127 .. +127
      *>     BINARY-SHORT  SIGNED   -32767 .. +32767
      *>     BINARY-LONG   SIGNED   -(2**31-1) .. +(2**31-1)
      *>     BINARY-DOUBLE SIGNED   -(2**63-1) .. +(2**63-1)
      *>     each UNSIGNED          0 .. 255 / 65535 / 2**32-1 /
      *>                            2**64-1
      *> and -2**(w-1) is OUTSIDE every signed one.  The exclusion is
      *> deliberate - it lets a sign-magnitude or ones' complement
      *> machine conform - and 15.58.4's NOTE says so from inside the
      *> standard: "BINARY-CHAR SIGNED | -128 (assuming an 8-bit
      *> twos-complement representation)".
      *>
      *> THE DETERMINATION THIS FILE PINS.  COBOL.NET provides the
      *> NATIVE TWO'S-COMPLEMENT band of each usage's byte width:
      *>     BINARY-CHAR   SIGNED   -128 .. +127
      *>     BINARY-CHAR   UNSIGNED 0 .. 255
      *>     BINARY-SHORT  SIGNED   -32768 .. +32767
      *>     BINARY-SHORT  UNSIGNED 0 .. 65535
      *>     BINARY-LONG   SIGNED   -2147483648 .. +2147483647
      *>     BINARY-LONG   UNSIGNED 0 .. 4294967295
      *>     BINARY-DOUBLE SIGNED   -9223372036854775808 ..
      *>                            +9223372036854775807
      *>     BINARY-DOUBLE UNSIGNED 0 .. 18446744073709551615
      *> Each SIGNED usage therefore expresses exactly ONE value
      *> beyond the GR12 minimum, -2**(w-1); the four UNSIGNED usages
      *> are exactly the minimum.  The band is written down ONCE, as
      *> the byte width per usage (PicInfo.StorageWidth) under the
      *> BinaryCapacity discipline (PicInfo.Truncation), and read by
      *> the runtime store bound (CobolNum.InBinaryRange /
      *> CobolNum.TryStoreU) and by the compile-time HIGHEST/LOWEST-
      *> ALGEBRAIC fold (IntrinsicBinder.BindAlgebraicFold), which
      *> AlgebraicFoldContainerAgreementTests holds in agreement.
      *>
      *> Part A - what GR12 REQUIRES: both endpoints of all EIGHT
      *> minimum bands round-trip.  The unsigned bands are CLOSED at 0
      *> ("0 <= n < 2**w"), so 0 is a required value and is measured
      *> rather than assumed.
      *> Part B - GR12's nesting sentence, at BOTH extremes of each
      *> narrower SIGNED usage, read off the usage itself through
      *> FUNCTION LOWEST-ALGEBRAIC (15.58.1 - "the lowest algebraic
      *> value that may be represented in argument-1") and FUNCTION
      *> HIGHEST-ALGEBRAIC (15.43.1 - "the greatest algebraic value
      *> that may be represented in argument-1"), so the leg stays
      *> correct whatever range the implementor provides.  The wider
      *> signed band is what puts this leg at risk: -2**(w-1) is
      *> exactly the value GR12's minimum does not carry.
      *> Part C - the A.1 item 206 measurement: both edges of all
      *> EIGHT provided bands, and one step beyond each.  IN-RANGE
      *> means the store completed; OUT-OF-RANGE means 14.7.5 raised
      *> the size error condition, whose case 3 is "if, after radix
      *> point alignment and any applicable rounding specifications,
      *> the result of an arithmetic statement is further from zero
      *> than permitted for the associated resultant data item" and
      *> whose GR2 states the same test as a MAGNITUDE one - "If the
      *> absolute value of the result of the arithmetic operation
      *> exceeds the maximum value allowed for any resultant
      *> identifier, the content of that resultant identifier is not
      *> changed" - so an ON SIZE ERROR firing IS the statement "this
      *> value is not expressible in this usage".
      *>
      *> THE FOUR UNSIGNED -1 LEGS REPORT WHICH VALUE LANDED, not
      *> merely IN-RANGE.  |-1| does not exceed any unsigned maximum,
      *> so 14.7.5 GR2's absolute-value test does not fire and the
      *> store completes; 14.9.8.4 GR2 then hands the value to 14.6.8
      *> for transfer into an item that has no sign position, and
      *> 14.9.25.4 GR6 d)2.b states the disposition in terms: "When an
      *> unsigned numeric item is the receiving item, the absolute
      *> value of the sending value is used, and no operational sign
      *> is generated for the receiving item".  "IN-RANGE" alone would
      *> not say what a negative sent to an unsigned container becomes.
      *>
      *> Every leg stores THROUGH a data item (WS-N), never a literal
      *> straight into the receiver, so no leg can pass by constant
      *> folding.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1BRB01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      *> SIGNED is the bare form (GR12); UNSIGNED must be written.
       01 BCS   USAGE BINARY-CHAR.
       01 BCU   USAGE BINARY-CHAR UNSIGNED.
       01 BSS   USAGE BINARY-SHORT.
       01 BSU   USAGE BINARY-SHORT UNSIGNED.
       01 BLS   USAGE BINARY-LONG.
       01 BLU   USAGE BINARY-LONG UNSIGNED.
       01 BDS   USAGE BINARY-DOUBLE.
       01 BDU   USAGE BINARY-DOUBLE UNSIGNED.
       01 WS-N  PIC S9(21) SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION.
       MAIN-PARA.
      *> Part A - the GR12 minimum band round-trips (REQUIRED).
           MOVE -127 TO WS-N
           MOVE WS-N TO BCS
           IF BCS = WS-N DISPLAY "CHAR-S-LO=OK"
           ELSE DISPLAY "CHAR-S-LO=BAD" END-IF
           MOVE 127 TO WS-N
           MOVE WS-N TO BCS
           IF BCS = WS-N DISPLAY "CHAR-S-HI=OK"
           ELSE DISPLAY "CHAR-S-HI=BAD" END-IF
           MOVE 0 TO WS-N
           MOVE WS-N TO BCU
           IF BCU = WS-N DISPLAY "CHAR-U-LO=OK"
           ELSE DISPLAY "CHAR-U-LO=BAD" END-IF
           MOVE 255 TO WS-N
           MOVE WS-N TO BCU
           IF BCU = WS-N DISPLAY "CHAR-U-HI=OK"
           ELSE DISPLAY "CHAR-U-HI=BAD" END-IF
           MOVE -32767 TO WS-N
           MOVE WS-N TO BSS
           IF BSS = WS-N DISPLAY "SHORT-S-LO=OK"
           ELSE DISPLAY "SHORT-S-LO=BAD" END-IF
           MOVE 32767 TO WS-N
           MOVE WS-N TO BSS
           IF BSS = WS-N DISPLAY "SHORT-S-HI=OK"
           ELSE DISPLAY "SHORT-S-HI=BAD" END-IF
           MOVE 0 TO WS-N
           MOVE WS-N TO BSU
           IF BSU = WS-N DISPLAY "SHORT-U-LO=OK"
           ELSE DISPLAY "SHORT-U-LO=BAD" END-IF
           MOVE 65535 TO WS-N
           MOVE WS-N TO BSU
           IF BSU = WS-N DISPLAY "SHORT-U-HI=OK"
           ELSE DISPLAY "SHORT-U-HI=BAD" END-IF
           MOVE -2147483647 TO WS-N
           MOVE WS-N TO BLS
           IF BLS = WS-N DISPLAY "LONG-S-LO=OK"
           ELSE DISPLAY "LONG-S-LO=BAD" END-IF
           MOVE 2147483647 TO WS-N
           MOVE WS-N TO BLS
           IF BLS = WS-N DISPLAY "LONG-S-HI=OK"
           ELSE DISPLAY "LONG-S-HI=BAD" END-IF
           MOVE 0 TO WS-N
           MOVE WS-N TO BLU
           IF BLU = WS-N DISPLAY "LONG-U-LO=OK"
           ELSE DISPLAY "LONG-U-LO=BAD" END-IF
           MOVE 4294967295 TO WS-N
           MOVE WS-N TO BLU
           IF BLU = WS-N DISPLAY "LONG-U-HI=OK"
           ELSE DISPLAY "LONG-U-HI=BAD" END-IF
           MOVE -9223372036854775807 TO WS-N
           MOVE WS-N TO BDS
           IF BDS = WS-N DISPLAY "DOUBLE-S-LO=OK"
           ELSE DISPLAY "DOUBLE-S-LO=BAD" END-IF
           MOVE 9223372036854775807 TO WS-N
           MOVE WS-N TO BDS
           IF BDS = WS-N DISPLAY "DOUBLE-S-HI=OK"
           ELSE DISPLAY "DOUBLE-S-HI=BAD" END-IF
           MOVE 0 TO WS-N
           MOVE WS-N TO BDU
           IF BDU = WS-N DISPLAY "DOUBLE-U-LO=OK"
           ELSE DISPLAY "DOUBLE-U-LO=BAD" END-IF
           MOVE 18446744073709551615 TO WS-N
           MOVE WS-N TO BDU
           IF BDU = WS-N DISPLAY "DOUBLE-U-HI=OK"
           ELSE DISPLAY "DOUBLE-U-HI=BAD" END-IF
      *> Part B - GR12's nesting sentence, at BOTH extremes of each
      *> narrower signed usage.
           MOVE FUNCTION LOWEST-ALGEBRAIC(BCS) TO WS-N
           MOVE WS-N TO BSS
           IF BSS = WS-N DISPLAY "NEST-CHAR-SHORT-LO=OK"
           ELSE DISPLAY "NEST-CHAR-SHORT-LO=BAD" END-IF
           MOVE FUNCTION HIGHEST-ALGEBRAIC(BCS) TO WS-N
           MOVE WS-N TO BSS
           IF BSS = WS-N DISPLAY "NEST-CHAR-SHORT-HI=OK"
           ELSE DISPLAY "NEST-CHAR-SHORT-HI=BAD" END-IF
           MOVE FUNCTION LOWEST-ALGEBRAIC(BSS) TO WS-N
           MOVE WS-N TO BLS
           IF BLS = WS-N DISPLAY "NEST-SHORT-LONG-LO=OK"
           ELSE DISPLAY "NEST-SHORT-LONG-LO=BAD" END-IF
           MOVE FUNCTION HIGHEST-ALGEBRAIC(BSS) TO WS-N
           MOVE WS-N TO BLS
           IF BLS = WS-N DISPLAY "NEST-SHORT-LONG-HI=OK"
           ELSE DISPLAY "NEST-SHORT-LONG-HI=BAD" END-IF
           MOVE FUNCTION LOWEST-ALGEBRAIC(BLS) TO WS-N
           MOVE WS-N TO BDS
           IF BDS = WS-N DISPLAY "NEST-LONG-DOUBLE-LO=OK"
           ELSE DISPLAY "NEST-LONG-DOUBLE-LO=BAD" END-IF
           MOVE FUNCTION HIGHEST-ALGEBRAIC(BLS) TO WS-N
           MOVE WS-N TO BDS
           IF BDS = WS-N DISPLAY "NEST-LONG-DOUBLE-HI=OK"
           ELSE DISPLAY "NEST-LONG-DOUBLE-HI=BAD" END-IF
      *> Part C - the Annex A.1 item 206 measurement, BOTH edges of
      *> all EIGHT provided bands and one step beyond each.  M<v> is
      *> the value -v and P<v> is +v.
           MOVE -128 TO WS-N
           COMPUTE BCS = WS-N
             ON SIZE ERROR DISPLAY "CHAR-S-M128=OUT-OF-RANGE"
             NOT ON SIZE ERROR DISPLAY "CHAR-S-M128=IN-RANGE"
           END-COMPUTE
           MOVE -129 TO WS-N
           COMPUTE BCS = WS-N
             ON SIZE ERROR DISPLAY "CHAR-S-M129=OUT-OF-RANGE"
             NOT ON SIZE ERROR DISPLAY "CHAR-S-M129=IN-RANGE"
           END-COMPUTE
           MOVE 128 TO WS-N
           COMPUTE BCS = WS-N
             ON SIZE ERROR DISPLAY "CHAR-S-P128=OUT-OF-RANGE"
             NOT ON SIZE ERROR DISPLAY "CHAR-S-P128=IN-RANGE"
           END-COMPUTE
           MOVE -1 TO WS-N
           COMPUTE BCU = WS-N
             ON SIZE ERROR DISPLAY "CHAR-U-M1=OUT-OF-RANGE"
             NOT ON SIZE ERROR
               IF BCU = 1
                 DISPLAY "CHAR-U-M1=IN-RANGE-ABS"
               ELSE
                 DISPLAY "CHAR-U-M1=IN-RANGE-OTHER"
               END-IF
           END-COMPUTE
           MOVE 256 TO WS-N
           COMPUTE BCU = WS-N
             ON SIZE ERROR DISPLAY "CHAR-U-P256=OUT-OF-RANGE"
             NOT ON SIZE ERROR DISPLAY "CHAR-U-P256=IN-RANGE"
           END-COMPUTE
           MOVE -32768 TO WS-N
           COMPUTE BSS = WS-N
             ON SIZE ERROR DISPLAY "SHORT-S-M32768=OUT-OF-RANGE"
             NOT ON SIZE ERROR DISPLAY "SHORT-S-M32768=IN-RANGE"
           END-COMPUTE
           MOVE -32769 TO WS-N
           COMPUTE BSS = WS-N
             ON SIZE ERROR DISPLAY "SHORT-S-M32769=OUT-OF-RANGE"
             NOT ON SIZE ERROR DISPLAY "SHORT-S-M32769=IN-RANGE"
           END-COMPUTE
           MOVE 32768 TO WS-N
           COMPUTE BSS = WS-N
             ON SIZE ERROR DISPLAY "SHORT-S-P32768=OUT-OF-RANGE"
             NOT ON SIZE ERROR DISPLAY "SHORT-S-P32768=IN-RANGE"
           END-COMPUTE
           MOVE -1 TO WS-N
           COMPUTE BSU = WS-N
             ON SIZE ERROR DISPLAY "SHORT-U-M1=OUT-OF-RANGE"
             NOT ON SIZE ERROR
               IF BSU = 1
                 DISPLAY "SHORT-U-M1=IN-RANGE-ABS"
               ELSE
                 DISPLAY "SHORT-U-M1=IN-RANGE-OTHER"
               END-IF
           END-COMPUTE
           MOVE 65536 TO WS-N
           COMPUTE BSU = WS-N
             ON SIZE ERROR DISPLAY "SHORT-U-P65536=OUT-OF-RANGE"
             NOT ON SIZE ERROR DISPLAY "SHORT-U-P65536=IN-RANGE"
           END-COMPUTE
           MOVE -2147483648 TO WS-N
           COMPUTE BLS = WS-N
             ON SIZE ERROR DISPLAY "LONG-S-M2E31=OUT-OF-RANGE"
             NOT ON SIZE ERROR DISPLAY "LONG-S-M2E31=IN-RANGE"
           END-COMPUTE
           MOVE -2147483649 TO WS-N
           COMPUTE BLS = WS-N
             ON SIZE ERROR DISPLAY "LONG-S-M2E31M1=OUT-OF-RANGE"
             NOT ON SIZE ERROR DISPLAY "LONG-S-M2E31M1=IN-RANGE"
           END-COMPUTE
           MOVE 2147483648 TO WS-N
           COMPUTE BLS = WS-N
             ON SIZE ERROR DISPLAY "LONG-S-P2E31=OUT-OF-RANGE"
             NOT ON SIZE ERROR DISPLAY "LONG-S-P2E31=IN-RANGE"
           END-COMPUTE
           MOVE -1 TO WS-N
           COMPUTE BLU = WS-N
             ON SIZE ERROR DISPLAY "LONG-U-M1=OUT-OF-RANGE"
             NOT ON SIZE ERROR
               IF BLU = 1
                 DISPLAY "LONG-U-M1=IN-RANGE-ABS"
               ELSE
                 DISPLAY "LONG-U-M1=IN-RANGE-OTHER"
               END-IF
           END-COMPUTE
           MOVE 4294967296 TO WS-N
           COMPUTE BLU = WS-N
             ON SIZE ERROR DISPLAY "LONG-U-P2E32=OUT-OF-RANGE"
             NOT ON SIZE ERROR DISPLAY "LONG-U-P2E32=IN-RANGE"
           END-COMPUTE
           MOVE -9223372036854775808 TO WS-N
           COMPUTE BDS = WS-N
             ON SIZE ERROR DISPLAY "DOUBLE-S-M2E63=OUT-OF-RANGE"
             NOT ON SIZE ERROR DISPLAY "DOUBLE-S-M2E63=IN-RANGE"
           END-COMPUTE
           MOVE -9223372036854775809 TO WS-N
           COMPUTE BDS = WS-N
             ON SIZE ERROR DISPLAY "DOUBLE-S-M2E63M1=OUT-OF-RANGE"
             NOT ON SIZE ERROR DISPLAY "DOUBLE-S-M2E63M1=IN-RANGE"
           END-COMPUTE
           MOVE 9223372036854775808 TO WS-N
           COMPUTE BDS = WS-N
             ON SIZE ERROR DISPLAY "DOUBLE-S-P2E63=OUT-OF-RANGE"
             NOT ON SIZE ERROR DISPLAY "DOUBLE-S-P2E63=IN-RANGE"
           END-COMPUTE
           MOVE -1 TO WS-N
           COMPUTE BDU = WS-N
             ON SIZE ERROR DISPLAY "DOUBLE-U-M1=OUT-OF-RANGE"
             NOT ON SIZE ERROR
               IF BDU = 1
                 DISPLAY "DOUBLE-U-M1=IN-RANGE-ABS"
               ELSE
                 DISPLAY "DOUBLE-U-M1=IN-RANGE-OTHER"
               END-IF
           END-COMPUTE
           MOVE 18446744073709551616 TO WS-N
           COMPUTE BDU = WS-N
             ON SIZE ERROR DISPLAY "DOUBLE-U-P2E64=OUT-OF-RANGE"
             NOT ON SIZE ERROR DISPLAY "DOUBLE-U-P2E64=IN-RANGE"
           END-COMPUTE
           STOP RUN.
